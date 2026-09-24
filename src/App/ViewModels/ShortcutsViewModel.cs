using System.Collections.ObjectModel;
using DofusSwitcher.Models;
using DofusSwitcher.Services;

// [WARN] App référence WinForms : Binding est ambigu (System.Windows.Forms.Binding). Alias explicite.
using Binding = DofusSwitcher.Models.Binding;

namespace DofusSwitcher.ViewModels;

/// <summary>
/// ViewModel de l'onglet Raccourcis (US-S04/S05) : associations de rotation « suivant/précédent » et
/// activation directe par compte. Chaque emplacement est un <see cref="ShortcutSlotViewModel"/> ; la
/// capture se fait via <see cref="IInputCaptureService"/> (aucun type WPF ici — testable).
/// [ARCH] Source de vérité unique : ce VM n'écrit jamais la config directement. Suivant/précédent
/// remontent au <see cref="MainViewModel"/> (top-level) ; l'activation directe passe par
/// <see cref="AccountsViewModel.SetDirectBinding"/> (sinon un réordonnancement écraserait le binding).
/// La détection de conflit réutilise l'unicité globale (data-model §Invariants) : une entrée déjà
/// assignée à une autre action est refusée avant persistance (RG-S05).
/// </summary>
public sealed class ShortcutsViewModel : ObservableObject
{
    private const string NextLabel = "Compte suivant";
    private const string PrevLabel = "Compte précédent";
    private const string NextKey = "\0next";
    private const string PrevKey = "\0prev";

    private readonly IInputCaptureService _capture;
    private readonly Action<Binding?> _applyNext;
    private readonly Action<Binding?> _applyPrev;
    private readonly Action<string, Binding?> _applyAccountDirect;
    private readonly Func<string, string?> _connectedCharacterOf;
    private AppConfig _config;

    /// <summary>Câble le VM sur la config courante, le service de capture et les chemins d'application.</summary>
    public ShortcutsViewModel(
        AppConfig config,
        IInputCaptureService capture,
        Action<Binding?> applyNext,
        Action<Binding?> applyPrev,
        Action<string, Binding?> applyAccountDirect,
        Func<string, string?> connectedCharacterOf)
    {
        _config = config;
        _capture = capture;
        _applyNext = applyNext;
        _applyPrev = applyPrev;
        _applyAccountDirect = applyAccountDirect;
        _connectedCharacterOf = connectedCharacterOf;

        NextSlot = CreateSlot(NextKey, NextLabel, applyNext);
        PrevSlot = CreateSlot(PrevKey, PrevLabel, applyPrev);
        OnConfigChanged(config);
    }

    /// <summary>Emplacement de bascule « suivant ».</summary>
    public ShortcutSlotViewModel NextSlot { get; }

    /// <summary>Emplacement de bascule « précédent ».</summary>
    public ShortcutSlotViewModel PrevSlot { get; }

    /// <summary>Emplacements d'activation directe : un par personnage lié à un compte, libellé par le compte (Axe 8).</summary>
    public ObservableCollection<ShortcutSlotViewModel> DirectSlots { get; } = [];

    /// <summary>Vrai s'il existe au moins un personnage lié pour l'activation directe (pilote l'état vide).</summary>
    public bool HasDirectSlots => DirectSlots.Count > 0;

    /// <summary>
    /// Réaligne l'affichage sur une nouvelle config (après toute persistance : suivant/précédent/direct
    /// ou réordonnancement des comptes). Appelé par le <see cref="MainViewModel"/>.
    /// </summary>
    public void OnConfigChanged(AppConfig config)
    {
        _config = config;
        NextSlot.SetBinding(config.NextBinding);
        PrevSlot.SetBinding(config.PrevBinding);
        SyncDirectSlots();
    }

    /// <summary>
    /// Rafraîchit les libellés d'activation directe quand l'état runtime change (un personnage lié se
    /// connecte/déconnecte) : le libellé suit le personnage connecté du compte. Appelé par le MainViewModel
    /// sur <see cref="AccountsViewModel.RuntimeChanged"/>.
    /// </summary>
    public void OnRuntimeChanged() => SyncDirectSlots();

    private ShortcutSlotViewModel CreateSlot(string key, string label, Action<Binding?> apply, string? id = null)
    {
        // Le slot se référence lui-même dans ses commandes (pour porter son message de conflit) : la
        // closure capture la variable, non nulle au moment où l'utilisateur déclenche la commande.
        ShortcutSlotViewModel slot = null!;
        slot = new ShortcutSlotViewModel(
            label,
            capture: () => CaptureInto(key, slot.Label, slot, apply),
            clear: () => ClearSlot(slot, apply),
            id: id);
        return slot;
    }

    private void CaptureInto(string slotKey, string actionLabel, ShortcutSlotViewModel slot, Action<Binding?> apply)
    {
        var captured = _capture.Capture(actionLabel);
        if (captured is null) return; // Échap → annulé, aucune modification (acceptance).

        var owner = FindConflictOwner(slotKey, captured);
        if (owner is not null)
        {
            // Refus RG-S05 : entrée déjà assignée à une autre action → conflit inline, pas de persistance.
            slot.ConflictMessage = $"Déjà assignée à « {owner} ». Choisir une autre entrée.";
            return;
        }

        slot.ConflictMessage = null;
        apply(captured); // → MainViewModel maj la config → OnConfigChanged rafraîchit l'affichage.
    }

    private void ClearSlot(ShortcutSlotViewModel slot, Action<Binding?> apply)
    {
        slot.ConflictMessage = null;
        apply(null);
    }

    /// <summary>
    /// Renvoie le libellé de l'action détenant déjà <paramref name="candidate"/> (hors l'emplacement
    /// <paramref name="slotKey"/> lui-même), ou <c>null</c> si l'entrée est libre. Réassigner la même
    /// entrée à son propre emplacement n'est pas un conflit.
    /// </summary>
    private string? FindConflictOwner(string slotKey, Binding candidate)
    {
        foreach (var (key, label, binding) in Assignments())
            if (key != slotKey && binding == candidate) return label;
        return null;
    }

    private IEnumerable<(string key, string label, Binding binding)> Assignments()
    {
        if (_config.NextBinding is not null) yield return (NextKey, NextLabel, _config.NextBinding);
        if (_config.PrevBinding is not null) yield return (PrevKey, PrevLabel, _config.PrevBinding);
        foreach (var game in _config.GameAccounts)
            if (game.DirectBinding is not null)
                // Conflit affiché avec le personnage connecté du compte si présent, sinon le nom du compte.
                yield return (DirectKey(game.Name), DirectLabel(game.Name), game.DirectBinding);
    }

    private static string DirectKey(string accountName) => "direct:" + accountName;

    /// <summary>Libellé d'un slot direct : le personnage connecté du compte si présent, sinon le nom du compte.</summary>
    private string DirectLabel(string accountName) => _connectedCharacterOf(accountName) ?? accountName;

    /// <summary>
    /// Réconcilie <see cref="DirectSlots"/> avec les <b>comptes ayant au moins un personnage lié</b> (Axe 8) :
    /// un slot <b>par compte</b> (l'entrée est liée au compte), <b>libellé par le personnage connecté</b>
    /// du compte (sinon le nom du compte). L'identité (clé) = nom du compte. Reconstruction en place.
    /// </summary>
    private void SyncDirectSlots()
    {
        var accounts = _config.GameAccounts
            .Where(a => _config.Accounts.Any(c => c.AccountName is not null && NameEquals(c.AccountName, a.Name)))
            .ToList();

        // 1. Retirer les slots dont le compte n'a plus de personnage lié (clé = nom du compte = Id).
        for (var i = DirectSlots.Count - 1; i >= 0; i--)
            if (!accounts.Any(a => NameEquals(a.Name, DirectSlots[i].Id!)))
                DirectSlots.RemoveAt(i);

        // 2. Ajouter/mettre à jour et ordonner ; libellé = personnage connecté, identité = nom du compte.
        for (var i = 0; i < accounts.Count; i++)
        {
            var account = accounts[i];
            var name = account.Name;
            var existing = DirectSlots.FirstOrDefault(s => NameEquals(s.Id!, name));
            if (existing is null)
            {
                existing = CreateSlot(DirectKey(name), DirectLabel(name), b => _applyAccountDirect(name, b), id: name);
                DirectSlots.Insert(i, existing);
            }
            else
            {
                var currentIndex = DirectSlots.IndexOf(existing);
                if (currentIndex != i) DirectSlots.Move(currentIndex, i);
            }
            existing.Label = DirectLabel(name);
            existing.SetBinding(account.DirectBinding);
        }

        OnPropertyChanged(nameof(HasDirectSlots));
    }

    private static bool NameEquals(string a, string b) => string.Equals(a, b, StringComparison.OrdinalIgnoreCase);
}
