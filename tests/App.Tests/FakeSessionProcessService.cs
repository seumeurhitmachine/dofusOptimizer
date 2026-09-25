using DofusSwitcher.Services;

namespace DofusSwitcher.Tests;

/// <summary>
/// Double de test pour <see cref="ISessionProcessService"/> : simule l'état du launcher et enregistre les
/// lancements/fermetures sans toucher au moindre processus réel. Permet d'asserter le gating de l'onglet
/// Comptes (bouton « Ouvrir une session ») et le routage des kills.
/// </summary>
public sealed class FakeSessionProcessService : ISessionProcessService
{
    /// <summary>État renvoyé par <see cref="IsLauncherRunning"/> (mutable pour simuler un launcher ouvert).</summary>
    public bool LauncherRunning { get; set; }

    /// <summary>Chemin renvoyé par <see cref="ResolveLauncherPath"/> (<c>null</c> = introuvable → bouton masqué).</summary>
    public string? LauncherPath { get; set; } = @"C:\fake\Ankama Launcher.exe";

    /// <summary>Valeur renvoyée par <see cref="LaunchLauncher"/>.</summary>
    public bool LaunchSucceeds { get; set; } = true;

    /// <summary>Nombre d'appels à <see cref="LaunchLauncher"/>.</summary>
    public int LaunchCount { get; private set; }

    /// <summary>Dernier chemin configuré reçu par <see cref="ResolveLauncherPath"/>.</summary>
    public string? LastConfiguredPath { get; private set; }

    /// <summary>Dernier chemin passé à <see cref="LaunchLauncher"/>.</summary>
    public string? LastLaunchedPath { get; private set; }

    /// <summary>Handles passés à <see cref="KillByHandle"/>, dans l'ordre.</summary>
    public List<nint> KilledHandles { get; } = [];

    public bool IsLauncherRunning() => LauncherRunning;

    /// <summary>Renvoie le chemin configuré s'il est non vide (priorité), sinon le chemin « auto-détecté » simulé.</summary>
    public string? ResolveLauncherPath(string? configuredPath)
    {
        LastConfiguredPath = configuredPath;
        return string.IsNullOrWhiteSpace(configuredPath) ? LauncherPath : configuredPath;
    }

    public bool LaunchLauncher(string launcherPath)
    {
        LaunchCount++;
        LastLaunchedPath = launcherPath;
        return LaunchSucceeds;
    }

    public void KillByHandle(nint hWnd) => KilledHandles.Add(hWnd);
}
