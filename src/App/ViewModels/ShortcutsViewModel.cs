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
    private readonly Action<string, Binding?> _applyCharacterDirect;
    private readonly Func<IReadOnlyList<string>> _connectedUnlinked;
    private AppConfig _config;

    /// <summary>Câble le VM sur la config courante, le service de capture et les chemins d'application.</summary>
    public ShortcutsViewModel(
        AppConfig config,
        IInputCaptureService capture,
        Action<Binding?> applyNext,
        Action<Binding?> applyPrev,
        Action<string, Binding?> applyAccountDirect,
        Action<string, Binding?> applyCharacterDirect,
        Func<IReadOnlyList<string>> connectedUnlinked)
    {
        _config = config;
        _capture = capture;
        _applyNext = applyNext;
        _applyPrev = applyPrev;
        _applyAccountDirect = applyAccountDirect;
        _applyCharacterDirect = applyCharacterDirect;
        _connectedUnlinked = connectedUnlinked;

        NextSlot = CreateSlot(NextKey, NextLabel, applyNext);
        PrevSlot = CreateSlot(PrevKey, PrevLabel, applyPrev);
        OnConfigChanged(config);
    }

    /// <summary>Emplacement de bascule « suivant ».</summary>
    public ShortcutSlotViewModel NextSlot { get; }

    /// <summary>Emplacement de bascule « précédent ».</summary>
    public ShortcutSlotViewModel PrevSlot { get; }

    /// <summary>
    /// Emplacements d'activation directe : un par compte ayant un personnage lié (Axe 8, libellé par le
    /// <b>compte</b> — entrée unique pour tous ses personnages) et un par personnage <b>connecté sans
    /// compte</b> (Axe 10, libellé par le personnage, entrée portée par <c>AccountConfig.DirectBinding</c>).
    /// </summary>
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
                // Conflit affiché avec le nom du compte (une seule entrée directe par compte, tous persos confondus).
                yield return (DirectKey(game.Name), DirectLabel(game.Name), game.DirectBinding);
        // Persos sans compte porteurs d'une entrée directe (Axe 10) : dans l'unicité globale (RG-S05).
        foreach (var character in _config.Accounts)
            if (character.AccountName is null && character.DirectBinding is not null)
                yield return (CharDirectKey(character.CharacterName), character.CharacterName, character.DirectBinding);
    }

    private static string DirectKey(string accountName) => "direct:" + accountName;

    private static string CharDirectKey(string characterName) => "chardirect:" + characterName;

    /// <summary>
    /// Libellé d'un slot direct de compte = <b>nom du compte</b> : l'entrée est unique pour tous les
    /// personnages du compte (elle active celui actuellement connecté), donc affichée par le compte.
    /// </summary>
    private static string DirectLabel(string accountName) => accountName;

    /// <summary>
    /// Réconcilie <see cref="DirectSlots"/> avec les <b>comptes ayant au moins un personnage lié</b> (Axe 8) :
    /// un slot <b>par compte</b> (l'entrée est liée au compte), <b>libellé par le personnage connecté</b>
    /// du compte (sinon le nom du compte). L'identité (clé) = nom du compte. Reconstruction en place.
    /// </summary>
    private void SyncDirectSlots()
    {
        var desired = new List<DirectSlotSpec>();

        // Comptes ayant ≥1 personnage lié (Axe 8) : entrée unique portée par le compte, libellé = nom du compte.
        foreach (var account in _config.GameAccounts)
        {
            if (!_config.Accounts.Any(c => c.AccountName is not null && NameEquals(c.AccountName, account.Name))) continue;
            var name = account.Name;
            desired.Add(new DirectSlotSpec(DirectKey(name), DirectLabel(name), account.DirectBinding, b => _applyAccountDirect(name, b)));
        }

        // Persos sans compte (Axe 10) : slot présent si connecté (assignable) OU déjà porteur d'une entrée
        // persistée (visible/effaçable même déconnecté). Ordre : connectés (rotation) puis liés-au-binding.
        var seen = new HashSet<string>(StringComparer.Ordinal);
        var characterNames = new List<string>();
        foreach (var name in _connectedUnlinked())
            if (seen.Add(name)) characterNames.Add(name);
        foreach (var character in _config.Accounts)
            if (character.AccountName is null && character.DirectBinding is not null && seen.Add(character.CharacterName))
                characterNames.Add(character.CharacterName);

        foreach (var name in characterNames)
        {
            var binding = _config.Accounts.FirstOrDefault(c => c.CharacterName == name)?.DirectBinding;
            desired.Add(new DirectSlotSpec(CharDirectKey(name), name, binding, b => _applyCharacterDirect(name, b)));
        }

        // Réconciliation en place par clé stable (Id = clé du slot).
        for (var i = DirectSlots.Count - 1; i >= 0; i--)
            if (!desired.Any(d => d.Key == DirectSlots[i].Id))
                DirectSlots.RemoveAt(i);

        for (var i = 0; i < desired.Count; i++)
        {
            var spec = desired[i];
            var existing = DirectSlots.FirstOrDefault(s => s.Id == spec.Key);
            if (existing is null)
            {
                existing = CreateSlot(spec.Key, spec.Label, spec.Apply, id: spec.Key);
                DirectSlots.Insert(i, existing);
            }
            else
            {
                var currentIndex = DirectSlots.IndexOf(existing);
                if (currentIndex != i) DirectSlots.Move(currentIndex, i);
            }
            existing.Label = spec.Label;
            existing.SetBinding(spec.Binding);
        }

        OnPropertyChanged(nameof(HasDirectSlots));
    }

    /// <summary>Spécification d'un emplacement direct désiré (clé stable, libellé, entrée courante, chemin d'application).</summary>
    private readonly record struct DirectSlotSpec(string Key, string Label, Binding? Binding, Action<Binding?> Apply);

    private static bool NameEquals(string a, string b) => string.Equals(a, b, StringComparison.OrdinalIgnoreCase);
}
