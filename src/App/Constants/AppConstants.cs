using System.IO;

namespace DofusSwitcher.Constants;

/// <summary>
/// Constantes applicatives centralisées : chemins, tunables, valeurs par défaut.
/// [ARCH] Aucun magic number ni chemin reconstruit ailleurs (archi §Règles de constantes).
/// </summary>
public static class AppConstants
{
    /// <summary>Dossier applicatif sous <c>%APPDATA%</c>.</summary>
    public const string AppFolderName = "DofusSwitcher";

    /// <summary>Nom du fichier de configuration JSON.</summary>
    public const string ConfigFileName = "config.json";

    /// <summary>
    /// Délai de débounce de l'autosave (ms). Une modification réarme le timer ; l'écriture
    /// disque n'a lieu qu'après ce silence, pour ne pas écrire à chaque frappe (archi §Threading).
    /// </summary>
    public const int AutosaveDebounceMs = 750;

    /// <summary>
    /// Index Win32 du bouton souris X2 (avant) — association <c>suivant</c> par défaut.
    /// Valeur <c>XBUTTON2</c> de user32 (data-model §Binding : <c>code</c> = index XButton).
    /// </summary>
    public const int DefaultNextButtonCode = 2;

    /// <summary>
    /// Index Win32 du bouton souris X1 (arrière) — association <c>précédent</c> par défaut.
    /// Valeur <c>XBUTTON1</c> de user32.
    /// </summary>
    public const int DefaultPrevButtonCode = 1;

    /// <summary>Chemin absolu du fichier de configuration : <c>%APPDATA%\DofusSwitcher\config.json</c>.</summary>
    public static string ConfigFilePath { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        AppFolderName,
        ConfigFileName);
}
