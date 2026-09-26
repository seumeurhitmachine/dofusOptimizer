using DofusSwitcher.Constants;
using Velopack;
using Velopack.Sources;

namespace DofusSwitcher.Services;

/// <summary>
/// Implémentation de <see cref="IUpdateService"/> au-dessus de Velopack, feed = GitHub Releases
/// (<see cref="AppConstants.UpdateFeedRepoUrl"/>).
/// [DECISION] Stratégie « télécharger puis appliquer à la fermeture » (<c>WaitExitThenApplyUpdates</c>) :
/// la MAJ s'installe silencieusement au prochain arrêt de l'app, sans redémarrage forcé ni popup — cohérent
/// avec l'esprit outil discret. En build de dev (non installé par l'installateur), Velopack signale
/// <c>IsInstalled = false</c> : on ne fait rien.
/// [WARN] Ce service ne touche pas l'UI (il pilote un process externe Update.exe) : pas de marshaling
/// Dispatcher requis. Toutes les erreurs (hors ligne, feed indisponible) sont avalées : une MAJ ratée ne
/// doit jamais dégrader le démarrage.
/// </summary>
public sealed class UpdateService : IUpdateService
{
    private readonly string _repoUrl;

    /// <summary>Crée le service ; <paramref name="repoUrl"/> par défaut = dépôt de release configuré.</summary>
    public UpdateService(string? repoUrl = null)
        => _repoUrl = repoUrl ?? AppConstants.UpdateFeedRepoUrl;

    /// <inheritdoc/>
    public async Task CheckAndStageAsync(CancellationToken ct = default)
    {
        try
        {
            var mgr = new UpdateManager(new GithubSource(_repoUrl, accessToken: null, prerelease: false));

            // Rien à faire si l'app n'a pas été installée via l'installateur (dev, exe copié à la main).
            if (!mgr.IsInstalled)
                return;

            var updateInfo = await mgr.CheckForUpdatesAsync().ConfigureAwait(false);
            if (updateInfo is null)
                return; // déjà à jour

            await mgr.DownloadUpdatesAsync(updateInfo).ConfigureAwait(false);

            // Applique la MAJ quand l'utilisateur quittera l'app (aucune interruption, pas de redémarrage forcé).
            mgr.WaitExitThenApplyUpdates(updateInfo);
        }
        catch
        {
            // Silencieux par conception : une vérification de MAJ ne doit jamais faire échouer le démarrage.
        }
    }
}
