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

    /// <summary>
    /// Titre visible de l'application (barre de fenêtre, info-bulle du tray). Texte centralisé
    /// (archi §Règles de constantes) — à extraire dans une <c>Strings</c> localisable si besoin.
    /// </summary>
    public const string AppTitle = "Dofus Window Switcher";

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

    /// <summary>
    /// Variable d'environnement activant le journal de détection (diagnostic). Présente (non vide) →
    /// la détection écrit chaque fenêtre énumérée + décision dans <see cref="DetectionLogFilePath"/>.
    /// </summary>
    public const string DebugEnvVar = "DOFUS_SWITCHER_DEBUG";

    /// <summary>Nom du fichier journal de détection (diagnostic opt-in).</summary>
    public const string DetectionLogFileName = "detection.log";

    /// <summary>Chemin absolu du journal de détection : <c>%APPDATA%\DofusSwitcher\detection.log</c>.</summary>
    public static string DetectionLogFilePath { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        AppFolderName,
        DetectionLogFileName);

    /// <summary>
    /// Clé de registre du démarrage automatique par utilisateur (RG-T05). <c>HKCU</c> — jamais
    /// <c>HKLM</c> ni le dossier <c>Startup</c> : pas d'élévation requise (ENF-005).
    /// </summary>
    public const string StartupRegistryKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";

    /// <summary>Nom de la valeur posée sous la clé <c>Run</c> pour le démarrage automatique.</summary>
    public const string StartupRegistryValueName = AppFolderName;

    /// <summary>Filtre des boîtes de dialogue export/import (JSON, US-P04).</summary>
    public const string ConfigFileDialogFilter = "Configuration JSON (*.json)|*.json|Tous les fichiers (*.*)|*.*";
}
