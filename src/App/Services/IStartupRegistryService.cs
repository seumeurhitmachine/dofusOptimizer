namespace DofusSwitcher.Services;

/// <summary>
/// Gère l'option « démarrer avec Windows » (RG-T05) via la clé <c>HKCU\...\Run</c>, sans élévation
/// (ENF-005). Abstrait derrière une interface pour rester testable (fake dans les tests, pas d'écriture
/// réelle du registre — l'implémentation concrète relève de la recette).
/// </summary>
public interface IStartupRegistryService
{
    /// <summary>Indique si l'entrée de démarrage automatique est actuellement posée.</summary>
    bool IsEnabled();

    /// <summary>
    /// Pose (<paramref name="enabled"/> vrai) ou retire l'entrée de démarrage automatique. Idempotent :
    /// poser réécrit le chemin de l'exécutable courant ; retirer sur une entrée absente ne fait rien.
    /// </summary>
    void SetEnabled(bool enabled);
}
