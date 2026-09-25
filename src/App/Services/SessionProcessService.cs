using System.Diagnostics;
using System.IO;
using Microsoft.Win32;
using DofusSwitcher.Constants;
using DofusSwitcher.Interop;

namespace DofusSwitcher.Services;

/// <summary>
/// Implémentation de <see cref="ISessionProcessService"/> via <c>System.Diagnostics.Process</c> (in-box,
/// aucun NuGet). [ARCH] Exception C-02 bornée (ref [DT-027]) : ce type est le seul à ouvrir un handle de
/// processus, et uniquement pour lancer le launcher ou tuer un client. Jamais de lecture mémoire/titre.
/// </summary>
public sealed class SessionProcessService : ISessionProcessService
{
    /// <inheritdoc/>
    public bool IsLauncherRunning()
    {
        // GetProcesses() énumère les process visibles par l'utilisateur courant ; on ne fait que lire leur
        // nom (aucun OpenProcess d'inspection). Le nom est comparé sans extension, insensible à la casse.
        foreach (var process in Process.GetProcesses())
        {
            try
            {
                if (process.ProcessName.Contains(AppConstants.AnkamaLauncherProcessName, StringComparison.OrdinalIgnoreCase))
                    return true;
            }
            catch
            {
                // Process disparu entre l'énumération et la lecture du nom : ignorer.
            }
            finally
            {
                process.Dispose();
            }
        }
        return false;
    }

    /// <inheritdoc/>
    public string? ResolveLauncherPath(string? configuredPath) =>
        ResolveLauncherPath(BuildCandidatePaths(configuredPath), File.Exists);

    /// <inheritdoc/>
    public bool LaunchLauncher(string launcherPath)
    {
        try
        {
            // UseShellExecute : laisse Windows résoudre l'exécutable comme un double-clic (pas de redirection).
            Process.Start(new ProcessStartInfo(launcherPath) { UseShellExecute = true })?.Dispose();
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <inheritdoc/>
    public void KillByHandle(nint hWnd)
    {
        if (hWnd == 0) return;

        var pid = NativeMethods.GetProcessIdFromWindow(hWnd);
        if (pid == 0) return;

        try
        {
            using var process = Process.GetProcessById((int)pid);
            process.Kill(); // force, sans confirmation
        }
        catch
        {
            // Process déjà terminé / accès refusé : rien à faire (le client a pu se fermer entre-temps).
        }
    }

    /// <summary>
    /// Sélection pure du chemin du launcher : premier candidat existant, ou <c>null</c>. Extraite pour être
    /// testable sans toucher au disque (<paramref name="exists"/> injectable).
    /// </summary>
    public static string? ResolveLauncherPath(IEnumerable<string> candidates, Func<string, bool> exists)
        => candidates.FirstOrDefault(exists);

    /// <summary>
    /// Candidats de chemin, dans l'ordre de préférence : chemin configuré (Réglages) d'abord, puis
    /// <c>%LOCALAPPDATA%\Programs\…</c>, puis registre <c>App Paths</c>.
    /// </summary>
    private static IEnumerable<string> BuildCandidatePaths(string? configuredPath)
    {
        if (!string.IsNullOrWhiteSpace(configuredPath))
            yield return configuredPath;

        var localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        if (!string.IsNullOrEmpty(localAppData))
            yield return Path.Combine(localAppData, AppConstants.AnkamaLauncherLocalAppDataPath);

        var fromRegistry = ReadAppPathsEntry();
        if (fromRegistry is not null)
            yield return fromRegistry;
    }

    /// <summary>
    /// Valeur par défaut de <c>App Paths\Ankama Launcher.exe</c> (HKCU puis HKLM), ou <c>null</c>. Windows y
    /// stocke le chemin complet de l'exécutable des applications installées.
    /// </summary>
    private static string? ReadAppPathsEntry()
    {
        var subKey = $@"SOFTWARE\Microsoft\Windows\CurrentVersion\App Paths\{AppConstants.AnkamaLauncherRegistryFallbackName}";
        foreach (var root in new[] { Registry.CurrentUser, Registry.LocalMachine })
        {
            try
            {
                using var key = root.OpenSubKey(subKey, writable: false);
                if (key?.GetValue(null) is string path && !string.IsNullOrWhiteSpace(path))
                    return path;
            }
            catch
            {
                // Ruche indisponible : passer au repli suivant.
            }
        }
        return null;
    }
}
