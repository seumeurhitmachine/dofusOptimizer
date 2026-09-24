using Microsoft.Win32;
using DofusSwitcher.Constants;

namespace DofusSwitcher.Services;

/// <summary>
/// Implémentation de <see cref="IStartupRegistryService"/> sur <c>HKEY_CURRENT_USER</c>.
/// [ARCH] Uniquement <c>HKCU\...\Run</c> (RG-T05, ENF-005) : écriture par utilisateur, aucune élévation.
/// Jamais <c>HKLM</c> ni le dossier <c>Startup</c>. <c>Microsoft.Win32.Registry</c> est in-box sur le TFM
/// <c>net10.0-windows</c> (aucun NuGet).
/// </summary>
public sealed class StartupRegistryService : IStartupRegistryService
{
    /// <inheritdoc/>
    public bool IsEnabled()
    {
        using var key = Registry.CurrentUser.OpenSubKey(AppConstants.StartupRegistryKeyPath, writable: false);
        return key?.GetValue(AppConstants.StartupRegistryValueName) is not null;
    }

    /// <inheritdoc/>
    public void SetEnabled(bool enabled)
    {
        // La clé Run existe toujours sous HKCU, mais CreateSubKey est idempotent et sans effet si présente.
        using var key = Registry.CurrentUser.CreateSubKey(AppConstants.StartupRegistryKeyPath, writable: true);
        if (enabled)
        {
            // [WARN] Guillemets autour du chemin : un exe dans un dossier avec espaces ne doit pas être
            // tronqué par le lanceur de session. Environment.ProcessPath = l'exe réel (single-file inclus).
            var exePath = Environment.ProcessPath;
            if (exePath is not null)
                key.SetValue(AppConstants.StartupRegistryValueName, $"\"{exePath}\"");
        }
        else if (key.GetValue(AppConstants.StartupRegistryValueName) is not null)
        {
            key.DeleteValue(AppConstants.StartupRegistryValueName);
        }
    }
}
