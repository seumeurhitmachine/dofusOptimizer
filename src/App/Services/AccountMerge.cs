using DofusSwitcher.Models;

namespace DofusSwitcher.Services;

/// <summary>
/// Fusion pure « config persistée + fenêtres détectées » → vue runtime ordonnée des comptes.
/// [ARCH] Fonction déterministe sans effet de bord ni type WPF : c'est le foyer testable de la
/// logique de rapprochement (spec §3.x, RG-D02/RG-D03). Le ViewModel se contente d'y (re)passer
/// l'état courant puis de réconcilier sa collection observable.
/// </summary>
public static class AccountMerge
{
    /// <summary>
    /// Rapproche par clé naturelle <c>characterName</c> les comptes persistés et les fenêtres
    /// détectées. Règles :
    /// <list type="bullet">
    /// <item>L'ordre des comptes persistés est la source de vérité (US-D03) et vient en premier.</item>
    /// <item>Un compte persisté est <c>connecté</c> si une fenêtre porte son nom, sinon <c>absent</c>
    /// mais conservé (RG-D02, RG-D03).</item>
    /// <item>Une fenêtre détectée sans compte persisté est ajoutée en fin, connectée et non exclue —
    /// runtime uniquement (aucune écriture en config à cet Axe ; la gestion arrive à l'Axe 4).</item>
    /// </list>
    /// </summary>
    public static IReadOnlyList<AccountRuntimeState> Merge(
        IReadOnlyList<AccountConfig> persisted,
        IReadOnlyCollection<DetectedWindow> detected)
    {
        // Index des fenêtres par nom ; en cas de doublon (rare), la première détectée fait foi.
        var byName = new Dictionary<string, DetectedWindow>(StringComparer.Ordinal);
        foreach (var window in detected)
            byName.TryAdd(window.CharacterName, window);

        var result = new List<AccountRuntimeState>(persisted.Count + byName.Count);
        var placed = new HashSet<string>(StringComparer.Ordinal);

        // 1. Comptes persistés, dans l'ordre de rotation : état calculé depuis les fenêtres.
        foreach (var account in persisted)
        {
            var isConnected = byName.TryGetValue(account.CharacterName, out var window);
            result.Add(new AccountRuntimeState(
                account.CharacterName,
                isConnected,
                account.Excluded,
                isConnected ? window.Handle : 0,
                account.AccountName));
            placed.Add(account.CharacterName);
        }

        // 2. Fenêtres détectées non encore persistées : ajoutées en fin, connectées (US-D01). Les clients
        //    SANS personnage (Axe 11) passent toujours par ici (jamais persistés) et portent HasCharacter=false.
        foreach (var window in detected)
        {
            if (!placed.Add(window.CharacterName)) continue; // déjà persistée ou doublon
            result.Add(new AccountRuntimeState(window.CharacterName, IsConnected: true, IsExcluded: false,
                window.Handle, AccountName: null, HasCharacter: window.HasCharacter));
        }

        return result;
    }
}
