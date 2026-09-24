using System.Collections.ObjectModel;
using DofusSwitcher.Models;
using DofusSwitcher.Services;

// [WARN] App référence WinForms : Binding est ambigu (System.Windows.Forms.Binding). Alias explicite.
using Binding = DofusSwitcher.Models.Binding;

namespace DofusSwitcher.ViewModels;

/// <summary>
/// ViewModel de l'onglet Comptes : liste temps réel des comptes avec leur état (US-D02), plus le
/// réordonnancement (US-D03) et l'exclusion/réintégration (US-D04, RG-D04).
/// S'abonne à <see cref="IWindowDetector"/> et, à chaque apparition/disparition, recalcule la vue via
/// <see cref="AccountMerge"/> puis réconcilie <see cref="Items"/> par clé <c>characterName</c> (les
/// instances d'items — donc la sélection — suivent le réordonnancement).
/// [ARCH] Aucun type WPF ni Dispatcher : le détecteur livre déjà sur le thread UI, ce VM reste testable
/// avec un faux détecteur. La persistance de l'ordre/exclusion sort par <see cref="AccountsChanged"/>,
/// que le <see cref="MainViewModel"/> relaie à l'autosave débouncé (ref [DT-006]).
/// [DECISION] Toute action utilisateur (réordonner/exclure) matérialise l'ordre visible complet dans
/// la config (ordre affiché = ordre persisté), y compris les comptes jusque-là seulement détectés
/// (ref [DT-009]). Avant toute action, un compte détecté non touché reste runtime only.
/// </summary>
public sealed class AccountsViewModel : ObservableObject
{
    private readonly Dictionary<string, DetectedWindow> _detected = new(StringComparer.Ordinal);
    private List<AccountConfig> _persisted;
    private AccountItemViewModel? _selectedItem;

    /// <summary>
    /// Câble le VM sur les comptes persistés et le détecteur. L'abonnement précède <see cref="IWindowDetector.Start"/>
    /// (composition root) pour capter l'énumération initiale.
    /// </summary>
    public AccountsViewModel(IReadOnlyList<AccountConfig> persisted, IWindowDetector detector)
    {
        _persisted = [.. persisted];
        MoveUpCommand = new RelayCommand(() => MoveSelected(-1), () => CanMoveSelected(-1));
        MoveDownCommand = new RelayCommand(() => MoveSelected(+1), () => CanMoveSelected(+1));
        ToggleExcludeCommand = new RelayCommand(ToggleExcludeSelected, () => SelectedItem is not null);
        ToggleExcludeItemCommand = new RelayCommand<AccountItemViewModel>(ToggleExclude);

        detector.AccountAppeared += OnAccountAppeared;
        detector.AccountDisappeared += OnAccountDisappeared;
        Rebuild(); // état initial : comptes persistés, tous absents tant que rien n'est détecté.
    }

    /// <summary>Comptes affichés, ordonnés (ordre persistant puis détectés non persistés en fin).</summary>
    public ObservableCollection<AccountItemViewModel> Items { get; } = [];

    /// <summary>Vrai quand aucun compte n'est connu (persisté ou détecté) : pilote l'état vide de la vue.</summary>
    public bool IsEmpty => Items.Count == 0;

    /// <summary>Inverse de <see cref="IsEmpty"/> : pilote la visibilité de la liste (converter in-box, sans inversion).</summary>
    public bool HasItems => Items.Count > 0;

    /// <summary>Compte sélectionné dans la liste — cible des commandes ↑/↓/exclure (lié à la ListBox).</summary>
    public AccountItemViewModel? SelectedItem
    {
        get => _selectedItem;
        set { if (SetProperty(ref _selectedItem, value)) RaiseCommandStates(); }
    }

    /// <summary>Monte le compte sélectionné d'un rang (US-D03). Désactivée si déjà en tête.</summary>
    public RelayCommand MoveUpCommand { get; }

    /// <summary>Descend le compte sélectionné d'un rang (US-D03). Désactivée si déjà en fin.</summary>
    public RelayCommand MoveDownCommand { get; }

    /// <summary>Bascule l'exclusion du compte sélectionné (US-D04). Désactivée sans sélection.</summary>
    public RelayCommand ToggleExcludeCommand { get; }

    /// <summary>
    /// Bascule l'exclusion d'un compte donné (US-D04), porté par le bouton « œil » de sa ligne — cible
    /// l'item passé en paramètre, indépendamment de la sélection courante.
    /// </summary>
    public RelayCommand<AccountItemViewModel> ToggleExcludeItemCommand { get; }

    /// <summary>
    /// Émis après une modification utilisateur (ordre/exclusion) avec la liste ordonnée à persister.
    /// La détection seule ne l'émet pas (l'état connecté/absent n'est jamais persisté).
    /// </summary>
    public event Action<IReadOnlyList<AccountConfig>>? AccountsChanged;

    /// <summary>
    /// Déplace un compte d'un index à un autre (glisser-déposer). Indices dans <see cref="Items"/>.
    /// [ARCH] Chemin unique de réordonnancement appelé par les handlers DnD du code-behind.
    /// </summary>
    public void MoveItem(int from, int to)
    {
        if (from == to || from < 0 || to < 0 || from >= Items.Count || to >= Items.Count) return;

        MaterializeFromItems(); // ordre visible → config (indices alignés sur Items)
        var moved = _persisted[from];
        _persisted.RemoveAt(from);
        _persisted.Insert(to, moved);
        RebuildAndPersist();
    }

    /// <summary>
    /// Remplace l'ensemble des comptes persistés (import de configuration, US-P04) et recalcule l'affichage.
    /// [DECISION] N'émet pas <see cref="AccountsChanged"/> : l'import est appliqué en bloc par le
    /// <see cref="MainViewModel"/>, qui persiste la config complète — éviter une double écriture. L'état
    /// runtime (comptes détectés/connectés) est préservé par la fusion sur les nouveaux comptes.
    /// </summary>
    public void LoadPersisted(IReadOnlyList<AccountConfig> persisted)
    {
        _persisted = [.. persisted];
        Rebuild();
    }

    /// <summary>
    /// Affecte (ou efface) l'activation directe d'un compte par son nom (Axe 5) — chemin unique de
    /// mutation du <c>DirectBinding</c>. Matérialise l'ordre visible d'abord (comme les autres actions,
    /// ref [DT-013]) pour que le compte, s'il n'était que détecté, soit persisté avec son binding, puis
    /// émet <see cref="AccountsChanged"/>. Sans compte correspondant : no-op.
    /// </summary>
    public void SetDirectBinding(string characterName, Binding? binding)
    {
        MaterializeFromItems();
        var index = _persisted.FindIndex(a => a.CharacterName == characterName);
        if (index < 0) return;
        _persisted[index] = _persisted[index] with { DirectBinding = binding };
        RebuildAndPersist();
    }

    private void OnAccountAppeared(DetectedWindow window)
    {
        _detected[window.CharacterName] = window;
        Rebuild();
    }

    private void OnAccountDisappeared(DetectedWindow window)
    {
        // Ne retirer que si le handle courant correspond : un renommage a déjà réémis l'apparition.
        if (_detected.TryGetValue(window.CharacterName, out var current) && current.Handle == window.Handle)
        {
            _detected.Remove(window.CharacterName);
            Rebuild();
        }
    }

    private bool CanMoveSelected(int direction)
    {
        if (SelectedItem is null) return false;
        var index = Items.IndexOf(SelectedItem);
        if (index < 0) return false;
        var target = index + direction;
        return target >= 0 && target < Items.Count;
    }

    private void MoveSelected(int direction)
    {
        if (SelectedItem is null) return;
        var index = Items.IndexOf(SelectedItem);
        MoveItem(index, index + direction);
    }

    private void ToggleExcludeSelected()
    {
        if (SelectedItem is not null) ToggleExclude(SelectedItem);
    }

    /// <summary>Bascule l'exclusion du compte donné (chemin partagé toolbar/sélection et bouton « œil » par ligne).</summary>
    private void ToggleExclude(AccountItemViewModel item)
    {
        MaterializeFromItems();
        var index = _persisted.FindIndex(a => a.CharacterName == item.CharacterName);
        if (index < 0) return;
        _persisted[index] = _persisted[index] with { Excluded = !_persisted[index].Excluded };
        RebuildAndPersist();
    }

    /// <summary>
    /// Fige l'ordre visible courant dans <see cref="_persisted"/> (ordre affiché = ordre persisté),
    /// matérialisant les comptes seulement détectés. Préserve <c>Excluded</c> et le <c>DirectBinding</c>
    /// existant par clé <c>characterName</c>.
    /// </summary>
    private void MaterializeFromItems()
    {
        var existingByName = _persisted.ToDictionary(a => a.CharacterName, StringComparer.Ordinal);
        var rebuilt = new List<AccountConfig>(Items.Count);
        foreach (var item in Items)
        {
            existingByName.TryGetValue(item.CharacterName, out var existing);
            rebuilt.Add(new AccountConfig(item.CharacterName, item.IsExcluded, existing?.DirectBinding));
        }
        _persisted = rebuilt;
    }

    private void RebuildAndPersist()
    {
        Rebuild();
        AccountsChanged?.Invoke(_persisted);
    }

    /// <summary>Recalcule la fusion et réconcilie <see cref="Items"/> en place (clé = nom de personnage).</summary>
    private void Rebuild()
    {
        var merged = AccountMerge.Merge(_persisted, _detected.Values);

        // 1. Retirer les comptes qui ne sont plus dans la fusion.
        for (var i = Items.Count - 1; i >= 0; i--)
            if (!merged.Any(m => m.CharacterName == Items[i].CharacterName))
                Items.RemoveAt(i);

        // 2. Ajouter/mettre à jour et ordonner selon la fusion.
        for (var i = 0; i < merged.Count; i++)
        {
            var state = merged[i];
            var existing = Items.FirstOrDefault(x => x.CharacterName == state.CharacterName);
            if (existing is null)
            {
                Items.Insert(i, new AccountItemViewModel(state));
            }
            else
            {
                existing.Apply(state);
                var currentIndex = Items.IndexOf(existing);
                if (currentIndex != i) Items.Move(currentIndex, i);
            }
        }

        // Numéro de rotation (1-based) = position dans la liste ; la vue regroupe les connectés en tête
        // sans toucher à cet ordre (le tri d'affichage retombe sur Number, ref AccountsView.xaml).
        for (var i = 0; i < Items.Count; i++)
            Items[i].Number = i + 1;

        OnPropertyChanged(nameof(IsEmpty));
        OnPropertyChanged(nameof(HasItems));
        RaiseCommandStates();
    }

    private void RaiseCommandStates()
    {
        MoveUpCommand.RaiseCanExecuteChanged();
        MoveDownCommand.RaiseCanExecuteChanged();
        ToggleExcludeCommand.RaiseCanExecuteChanged();
    }
}
