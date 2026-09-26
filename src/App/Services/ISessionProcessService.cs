namespace DofusSwitcher.Services;

/// <summary>
/// Cycle de session (Axe 9) : lance l'Ankama Launcher et ferme (force-kill) les processus des clients
/// DOFUS. C'est le <b>seul</b> point du code autorisé à ouvrir un handle de processus (ref [DT-027]) —
/// exception explicite et bornée à C-02. Aucune injection, lecture mémoire, entrée synthétique (C-03) ni
/// inspection du jeu : uniquement <c>Process.Start</c> (launcher) et <c>Process.Kill</c> (clients, via le
/// PID obtenu depuis le HWND).
/// [ARCH] Abstrait derrière une interface pour rester testable (fake dans les tests — aucun process réel ;
/// l'implémentation concrète relève de la recette), comme <see cref="IStartupRegistryService"/> (ref [DT-023]).
/// </summary>
public interface ISessionProcessService
{
    /// <summary>Vrai si un processus « Ankama Launcher » est actuellement ouvert (pilote le bouton « Ouvrir une session »).</summary>
    bool IsLauncherRunning();

    /// <summary>
    /// Chemin absolu utilisable de l'exécutable du launcher, ou <c>null</c> si aucun. Ordre : le
    /// <paramref name="configuredPath"/> renseigné dans les Réglages (s'il existe sur le disque) prime, sinon
    /// auto-détection (<c>%LOCALAPPDATA%\Programs\…</c> + registre). <c>null</c> → bouton « Ouvrir une session » masqué.
    /// </summary>
    string? ResolveLauncherPath(string? configuredPath);

    /// <summary>Lance l'Ankama Launcher au chemin donné. Renvoie <c>false</c> si le démarrage échoue.</summary>
    bool LaunchLauncher(string launcherPath);

    /// <summary>
    /// Ramène la fenêtre de l'Ankama Launcher déjà ouvert au premier plan (Axe 11) — utilisé par « Ouvrir une
    /// session » quand le launcher tourne déjà, pour l'afficher plutôt que d'en relancer un. Renvoie <c>false</c>
    /// si aucun launcher avec fenêtre n'est trouvé. Ref exception C-02 [DT-027] (cycle de session, pas le jeu).
    /// </summary>
    bool TryActivateLauncher();

    /// <summary>
    /// Force-kill le processus propriétaire de la fenêtre <paramref name="hWnd"/> (client connecté).
    /// Silencieux si le PID est nul ou le processus déjà terminé. Sans confirmation (cohérent RG-C04).
    /// </summary>
    void KillByHandle(nint hWnd);
}
