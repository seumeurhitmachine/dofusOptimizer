using System.Collections.ObjectModel;
using DofusSwitcher.Models;
using DofusSwitcher.Services;

namespace DofusSwitcher.ViewModels;

/// <summary>
/// ViewModel de l'onglet Comptes : liste temps réel des comptes avec leur état (US-D02).
/// S'abonne à <see cref="IWindowDetector"/> et, à chaque apparition/disparition, recalcule la vue
/// via <see cref="AccountMerge"/> puis réconcilie <see cref="Items"/> par clé <c>characterName</c>.
/// [ARCH] Aucun type WPF ni Dispatcher : le détecteur livre déjà ses événements sur le thread UI, si
/// bien que ce VM reste testable avec un faux détecteur (archi §MVVM/§Threading). Lecture seule à cet
/// Axe — réordonnancement/exclusion à l'Axe 4.
/// </summary>
public sealed class AccountsViewModel : ObservableObject
{
    private readonly IReadOnlyList<AccountConfig> _persisted;
    private readonly Dictionary<string, DetectedWindow> _detected = new(StringComparer.Ordinal);

    /// <summary>
    /// Câble le VM sur les comptes persistés et le détecteur fourni. L'abonnement précède l'appel à
    /// <see cref="IWindowDetector.Start"/> (fait par la composition root) pour capter l'énumération
    /// initiale.
    /// </summary>
    public AccountsViewModel(IReadOnlyList<AccountConfig> persisted, IWindowDetector detector)
    {
        _persisted = persisted;
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

        OnPropertyChanged(nameof(IsEmpty));
        OnPropertyChanged(nameof(HasItems));
    }
}
