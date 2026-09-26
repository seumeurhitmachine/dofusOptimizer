namespace DofusSwitcher.Services;

/// <summary>
/// Recherche, télécharge et prépare l'installation d'une mise à jour de l'application (auto-update).
/// [ARCH] Abstrait derrière une interface pour rester testable et découplé de Velopack (mock dans les
/// tests). L'application effective est différée à la prochaine fermeture — aucune interruption utilisateur.
/// </summary>
public interface IUpdateService
{
    /// <summary>
    /// Vérifie s'il existe une version plus récente sur le feed de release ; si oui, la télécharge et
    /// planifie son installation à la prochaine sortie de l'app. Silencieux et sans effet lorsque l'app
    /// n'est pas installée via l'installateur (build de dev) ou en cas d'erreur réseau — ne lève jamais.
    /// </summary>
    Task CheckAndStageAsync(CancellationToken ct = default);
}
