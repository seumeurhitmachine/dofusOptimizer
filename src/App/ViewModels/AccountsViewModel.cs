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
    private readonly ISessionProcessService? _session;
    private readonly Action? _requestShutdown;
    private List<AccountConfig> _persisted;
    private List<GameAccount> _gameAccounts;
    private AccountItemViewModel? _selectedItem;
    private bool _hasConnectedClients;
    private bool _showOpenSession;
    private string? _configuredLauncherPath;
    private string? _launcherPath;
    private bool _launcherResolved;

    /// <summary>
    /// Câble le VM sur les comptes persistés et le détecteur. L'abonnement précède <see cref="IWindowDetector.Start"/>
    /// (composition root) pour capter l'énumération initiale. Le service de session et le rappel de fermeture
    /// (Axe 9) sont optionnels : les tests VM qui n'exercent pas le cycle de session les omettent.
    /// </summary>
    public AccountsViewModel(
        IReadOnlyList<AccountConfig> persisted,
        IWindowDetector detector,
        IReadOnlyList<GameAccount>? gameAccounts = null,
        ISessionProcessService? session = null,
        Action? requestShutdown = null)
    {
        _persisted = [.. persisted];
        _gameAccounts = [.. gameAccounts ?? []];
        _session = session;
        _requestShutdown = requestShutdown;
        MoveUpCommand = new RelayCommand(() => MoveSelected(-1), () => CanMoveSelected(-1));
        MoveDownCommand = new RelayCommand(() => MoveSelected(+1), () => CanMoveSelected(+1));
        ToggleExcludeCommand = new RelayCommand(ToggleExcludeSelected, () => SelectedItem is not null);
        ToggleExcludeItemCommand = new RelayCommand<AccountItemViewModel>(ToggleExclude);
        OpenSessionCommand = new RelayCommand(OpenSession, () => _session is not null && LauncherPath() is not null);
        CloseClientCommand = new RelayCommand<AccountItemViewModel>(CloseClient);
        EndSessionCommand = new RelayCommand(EndSession, () => HasConnectedClients);

        detector.AccountAppeared += OnAccountAppeared;
        detector.AccountDisappeared += OnAccountDisappeared;
        Rebuild(); // état initial : comptes persistés, tous absents tant que rien n'est détecté.
    }

    /// <summary>Zone 1 — comptes ayant un personnage lié actuellement connecté (compte + personnage).</summary>
    public ObservableCollection<ConnectedAccountViewModel> ConnectedAccounts { get; } = [];

    /// <summary>Zone 2 — personnages connectés non rattachés à un compte (avec action de liaison).</summary>
    public ObservableCollection<UnlinkedCharacterViewModel> UnlinkedConnected { get; } = [];

    /// <summary>Zone 3 — noms des comptes ayant des personnages liés mais aucun connecté (nom seul).</summary>
    public ObservableCollection<string> DisconnectedAccounts { get; } = [];

    /// <summary>Comptes disponibles pour la liaison (sans personnage lié connecté, RG-C03) — partagée par les lignes de zone 2.</summary>
    public ObservableCollection<string> AvailableAccounts { get; } = [];

    /// <summary>Émis après recomposition des zones (changement d'état runtime) — l'onglet Raccourcis rafraîchit ses libellés.</summary>
    public event Action? RuntimeChanged;

    /// <summary>Nom du personnage lié actuellement connecté pour un compte donné, ou <c>null</c> si aucun (activation directe, Axe 8).</summary>
    public string? ConnectedCharacterOf(string accountName) =>
        ConnectedAccounts.FirstOrDefault(c => string.Equals(c.AccountName, accountName, StringComparison.OrdinalIgnoreCase))?.Character.CharacterName;

    /// <summary>Comptes affichés, ordonnés (ordre persistant puis détectés non persistés en fin).</summary>
    public ObservableCollection<AccountItemViewModel> Items { get; } = [];

    /// <summary>Vrai quand aucun compte n'est connu (persisté ou détecté) : pilote l'état vide de la vue.</summary>
    public bool IsEmpty => Items.Count == 0;

    /// <summary>Inverse de <see cref="IsEmpty"/> : pilote la visibilité de la liste (converter in-box, sans inversion).</summary>
    public bool HasItems => Items.Count > 0;

    /// <summary>Zone 1 non vide (au moins un compte connecté) — pilote la visibilité de la section.</summary>
    public bool HasConnected => ConnectedAccounts.Count > 0;

    /// <summary>Zone 2 non vide (au moins un personnage connecté sans compte).</summary>
    public bool HasUnlinked => UnlinkedConnected.Count > 0;

    /// <summary>Zone 3 non vide (au moins un compte déconnecté à personnages liés).</summary>
    public bool HasDisconnected => DisconnectedAccounts.Count > 0;

    /// <summary>Vrai si au moins un client DOFUS est actuellement connecté (Axe 9) — pilote « Terminer session ».</summary>
    public bool HasConnectedClients
    {
        get => _hasConnectedClients;
        private set { if (SetProperty(ref _hasConnectedClients, value)) EndSessionCommand.RaiseCanExecuteChanged(); }
    }

    /// <summary>
    /// Vrai quand aucun client n'est connecté ET aucun launcher n'est ouvert (Axe 9) : pilote la visibilité
    /// du gros bouton « Ouvrir une session ». Sans service de session (tests VM purs), toujours faux.
    /// </summary>
    public bool ShowOpenSession
    {
        get => _showOpenSession;
        private set => SetProperty(ref _showOpenSession, value);
    }

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
    /// Lance l'Ankama Launcher (Axe 9, cycle de session). Désactivée sans service de session ou si le chemin
    /// du launcher est introuvable (bouton affiché mais grisé). Ref exception C-02 [DT-027].
    /// </summary>
    public RelayCommand OpenSessionCommand { get; }

    /// <summary>Force-kill le processus du client passé en paramètre (croix rouge par ligne connectée, Axe 9).</summary>
    public RelayCommand<AccountItemViewModel> CloseClientCommand { get; }

    /// <summary>Force-kill tous les clients connectés puis ferme réellement l'application (Axe 9).</summary>
    public RelayCommand EndSessionCommand { get; }

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
            rebuilt.Add(new AccountConfig(item.CharacterName, item.IsExcluded, existing?.DirectBinding, existing?.AccountName));
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

        RebuildZones();
        OnPropertyChanged(nameof(IsEmpty));
        OnPropertyChanged(nameof(HasItems));
        RaiseCommandStates();
    }

    /// <summary>
    /// Recompose les 3 zones (comptes-v2 §2.6) et la liste des comptes disponibles à partir de l'état
    /// runtime courant (<see cref="Items"/>) et des comptes déclarés. Reconstruction complète : les
    /// collections sont petites (nombre de clients/comptes) et les changements peu fréquents.
    /// </summary>
    private void RebuildZones()
    {
        var states = Items
            .Select(i => new AccountRuntimeState(i.CharacterName, i.IsConnected, i.IsExcluded, i.Handle, i.AccountName))
            .ToList();
        var zones = AccountZones.Build(_gameAccounts, states);

        // Comptes disponibles : mis à jour en place (référence partagée par les lignes de zone 2).
        AvailableAccounts.Clear();
        foreach (var name in zones.AvailableAccounts) AvailableAccounts.Add(name);

        // Zone 1 ordonnée par ordre de rotation (ordre de Items), pas par ordre de déclaration des comptes.
        var connectedByChar = zones.Connected.ToDictionary(z => z.ConnectedCharacter, StringComparer.Ordinal);
        ConnectedAccounts.Clear();
        foreach (var item in Items)
            if (connectedByChar.TryGetValue(item.CharacterName, out var z))
                ConnectedAccounts.Add(new ConnectedAccountViewModel(z.AccountName, item));

        UnlinkedConnected.Clear();
        foreach (var name in zones.UnlinkedConnected)
        {
            var character = Items.FirstOrDefault(i => i.CharacterName == name);
            if (character is not null)
                UnlinkedConnected.Add(new UnlinkedCharacterViewModel(character, AvailableAccounts, LinkCharacter));
        }

        DisconnectedAccounts.Clear();
        foreach (var z in zones.Disconnected)
            DisconnectedAccounts.Add(z.AccountName);

        OnPropertyChanged(nameof(HasConnected));
        OnPropertyChanged(nameof(HasUnlinked));
        OnPropertyChanged(nameof(HasDisconnected));
        RefreshSessionState();
        RuntimeChanged?.Invoke();
    }

    /// <summary>
    /// Recalcule l'état du cycle de session (Axe 9) : présence de clients connectés et visibilité du bouton
    /// « Ouvrir une session ». [DECISION] La présence du launcher n'est sondée que lorsqu'aucun client n'est
    /// connecté (sinon le bouton est masqué de toute façon) et jamais en polling (anti-bot) : le
    /// rafraîchissement suit les apparitions/disparitions de fenêtres. Limitation : un launcher ouvert
    /// hors application, sans client, n'est détecté qu'au prochain événement fenêtre → recette.
    /// </summary>
    private void RefreshSessionState()
    {
        HasConnectedClients = Items.Any(i => i.IsConnected);
        var launcherRunning = !HasConnectedClients && _session is not null && _session.IsLauncherRunning();
        // Masqué si aucun client, launcher déjà ouvert, OU aucun chemin de launcher utilisable (RG évolution).
        ShowOpenSession = _session is not null && !HasConnectedClients && !launcherRunning && LauncherPath() is not null;
    }

    /// <summary>
    /// Chemin du launcher résolu une fois puis mémoïsé : chemin configuré (Réglages) sinon auto-détection.
    /// Réévalué par <see cref="SetLauncherPath"/> quand l'utilisateur change le chemin.
    /// </summary>
    private string? LauncherPath()
    {
        if (!_launcherResolved)
        {
            _launcherPath = _session?.ResolveLauncherPath(_configuredLauncherPath);
            _launcherResolved = true;
        }
        return _launcherPath;
    }

    /// <summary>
    /// Applique le chemin de launcher configuré (Réglages, Axe 9) : réévalue la résolution et le gating du
    /// bouton. Appelé au démarrage (config initiale), sur modification dans les Réglages et à l'import.
    /// </summary>
    public void SetLauncherPath(string? configuredPath)
    {
        _configuredLauncherPath = configuredPath;
        _launcherResolved = false;
        RefreshSessionState();
        OpenSessionCommand.RaiseCanExecuteChanged();
    }

    private void OpenSession()
    {
        var path = LauncherPath();
        if (_session is null || path is null || !_session.LaunchLauncher(path)) return;
        // Le launcher vient de démarrer : masquer le bouton sans attendre le prochain événement fenêtre.
        ShowOpenSession = false;
    }

    private void CloseClient(AccountItemViewModel? item)
    {
        if (item is not null) _session?.KillByHandle(item.Handle);
    }

    private void EndSession()
    {
        if (_session is null) return;
        // Snapshot : KillByHandle peut faire disparaître des fenêtres et muter Items pendant l'itération.
        foreach (var item in Items.Where(i => i.IsConnected).ToList())
            _session.KillByHandle(item.Handle);
        _requestShutdown?.Invoke();
    }

    /// <summary>
    /// Remplace la liste des comptes déclarés (après CRUD via le <see cref="MainViewModel"/>) et recompose
    /// les zones. Ne touche pas aux personnages : seul l'affichage des zones dépend des comptes.
    /// </summary>
    public void LoadGameAccounts(IReadOnlyList<GameAccount> gameAccounts)
    {
        _gameAccounts = [.. gameAccounts];
        RebuildZones();
    }

    /// <summary>
    /// Lie (ou délie si <paramref name="accountName"/> vaut <c>null</c>) un personnage à un compte — chemin
    /// unique de mutation du lien. Matérialise l'ordre visible (comme <see cref="SetDirectBinding"/>, ref
    /// [DT-013]) puis émet <see cref="AccountsChanged"/>. Sans personnage correspondant : no-op.
    /// </summary>
    public void LinkCharacter(string characterName, string? accountName)
    {
        MaterializeFromItems();
        var index = _persisted.FindIndex(a => a.CharacterName == characterName);
        if (index < 0) return;
        _persisted[index] = _persisted[index] with { AccountName = accountName };
        RebuildAndPersist();
    }

    private void RaiseCommandStates()
    {
        MoveUpCommand.RaiseCanExecuteChanged();
        MoveDownCommand.RaiseCanExecuteChanged();
        ToggleExcludeCommand.RaiseCanExecuteChanged();
    }
}
