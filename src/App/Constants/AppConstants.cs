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
    public const string AppTitle = "Dofus Optimizer";

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

    // --- Cycle de session (Axe 9) : lancement du launcher / fermeture des clients. ---

    /// <summary>
    /// Fragment de nom de processus de l'Ankama Launcher (comparaison insensible à la casse, sans
    /// extension). Sert à détecter si le launcher est déjà ouvert (bouton « Ouvrir une session »).
    /// </summary>
    public const string AnkamaLauncherProcessName = "Ankama Launcher";

    /// <summary>
    /// Chemin relatif de l'exécutable du launcher sous <c>%LOCALAPPDATA%</c> (installation par défaut réelle :
    /// <c>%LOCALAPPDATA%\Programs\Ankama Launcher\Ankama Launcher.exe</c> — le dossier <c>Ankama</c> ne contient
    /// que les jeux). Auto-détection de repli si aucun chemin n'est configuré (ref <see cref="AnkamaLauncherRegistryFallbackName"/>).
    /// </summary>
    public static string AnkamaLauncherLocalAppDataPath { get; } = Path.Combine(
        "Programs", "Ankama Launcher", "Ankama Launcher.exe");

    /// <summary>Nom de l'exécutable recherché dans le registre <c>App Paths</c> (auto-détection du chemin).</summary>
    public const string AnkamaLauncherRegistryFallbackName = "Ankama Launcher.exe";

    /// <summary>Filtre de la boîte de dialogue de sélection de l'exécutable du launcher (Réglages, Axe 9).</summary>
    public const string LauncherFileDialogFilter =
        "Ankama Launcher (Ankama Launcher.exe)|Ankama Launcher.exe|Exécutables (*.exe)|*.exe|Tous les fichiers (*.*)|*.*";

    // --- Instance unique (Axe 9) : objets kernel nommés (namespace session, isolés par utilisateur). ---

    /// <summary>Nom du mutex garantissant une seule instance ; le GUID évite toute collision avec un autre programme.</summary>
    public const string SingleInstanceMutexName = "DofusOptimizer.SingleInstance.{6C2F1A94-2D7B-4E52-9A1F-DF0B1E3C77A2}";

    /// <summary>Nom de l'événement signalé par une 2ᵉ instance pour réveiller/afficher la fenêtre de l'instance en cours.</summary>
    public const string ShowWindowEventName = "DofusOptimizer.ShowWindow.{6C2F1A94-2D7B-4E52-9A1F-DF0B1E3C77A2}";

    /// <summary>Lien de soutien (Ko-fi) ouvert par le bouton « Soutenir » de l'onglet Paramètres.</summary>
    public const string SupportUrl = "https://ko-fi.com/seumeurhitmatchine";

    // --- Mises à jour automatiques (Velopack, packaging). ---

    /// <summary>
    /// Dépôt GitHub source des mises à jour automatiques. Les artefacts de release (installateur +
    /// packages + fichiers RELEASES) sont publiés dans les GitHub Releases de ce dépôt ; l'app y cherche
    /// une version plus récente au démarrage (<see cref="Services.IUpdateService"/>). Réf : docs/packaging.md.
    /// </summary>
    public const string UpdateFeedRepoUrl = "https://github.com/seumeurhitmachine/dofusOptimizer";
}
