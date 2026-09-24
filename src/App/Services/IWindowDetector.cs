using DofusSwitcher.Models;

namespace DofusSwitcher.Services;

/// <summary>
/// Observe les fenêtres du bureau et signale l'apparition/disparition des clients DOFUS.
/// [ARCH] Détection événementielle (WinEvent hook), aucun polling (RG-D05). Le service ignore le
/// ViewModel et l'UI : la communication est montante, par événement (archi §Découpage en couches).
/// Les événements sont levés sur le thread UI (le détecteur marshale les callbacks natifs).
/// </summary>
public interface IWindowDetector : IDisposable
{
    /// <summary>Levé quand une fenêtre DOFUS est repérée (énumération initiale ou création/login).</summary>
    event Action<DetectedWindow>? AccountAppeared;

    /// <summary>Levé quand une fenêtre DOFUS connue disparaît (fermeture, logout, renommage).</summary>
    event Action<DetectedWindow>? AccountDisappeared;

    /// <summary>
    /// Démarre la détection : énumération initiale des fenêtres puis installation du hook temps réel.
    /// [WARN] À appeler sur le thread UI (le hook OUTOFCONTEXT poste ses événements dans la file de
    /// messages du thread appelant).
    /// </summary>
    void Start();

    /// <summary>Arrête le hook et libère les ressources natives. Idempotent.</summary>
    void Stop();
}
