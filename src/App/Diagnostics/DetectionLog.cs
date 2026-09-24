using System.IO;
using DofusSwitcher.Constants;

namespace DofusSwitcher.Diagnostics;

/// <summary>
/// Journal de diagnostic de la détection, opt-in via la variable d'environnement
/// <see cref="AppConstants.DebugEnvVar"/>. Écrit chaque fenêtre énumérée (titre/classe/visibilité) et
/// chaque décision de reconnaissance dans <see cref="AppConstants.DetectionLogFilePath"/>.
/// [DECISION] Diagnostic local par fichier (pas de dépendance de logging, zéro NuGet) : permet de voir
/// les vrais titres des fenêtres clientes quand la reconnaissance échoue. Coût nul quand désactivé
/// (<see cref="IsEnabled"/> court-circuite tout). Purement local — aucune interaction avec le jeu.
/// </summary>
internal static class DetectionLog
{
    private static readonly object Gate = new();

    /// <summary>Vrai si la variable d'environnement de debug est présente et non vide.</summary>
    public static bool IsEnabled { get; } =
        !string.IsNullOrEmpty(Environment.GetEnvironmentVariable(AppConstants.DebugEnvVar));

    /// <summary>Ajoute une ligne horodatée au journal. Best-effort : un échec d'écriture est ignoré.</summary>
    public static void Write(string message)
    {
        if (!IsEnabled) return;
        try
        {
            lock (Gate)
            {
                Directory.CreateDirectory(Path.GetDirectoryName(AppConstants.DetectionLogFilePath)!);
                File.AppendAllText(
                    AppConstants.DetectionLogFilePath,
                    $"{DateTime.Now:HH:mm:ss.fff}  {message}{Environment.NewLine}");
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // Diagnostic best-effort : ne jamais faire échouer la détection à cause du journal.
        }
    }
}
