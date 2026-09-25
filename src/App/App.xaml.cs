using System.Windows;
using DofusSwitcher.Persistence;
using DofusSwitcher.Services;
using DofusSwitcher.Tray;
using DofusSwitcher.ViewModels;
using DofusSwitcher.Views;

// [WARN] UseWPF + UseWindowsForms exposent deux types Application (WPF et WinForms) via les
// global usings : lever l'ambiguïté en faveur de WPF pour tout ce fichier.
using Application = System.Windows.Application;

namespace DofusSwitcher;

/// <summary>
/// Composition root de l'application.
/// [ARCH] Toutes les instances sont créées ici, une fois, dans l'ordre de dépendance
/// (persistance → services → ViewModel → View → tray). Pas de conteneur DI.
/// Réf : ARCHI-DOTNET-WPF.md §Composition root.
/// </summary>
public partial class App : Application
{
    private ConfigAutosaveService? _autosave;
    private IWindowDetector? _windowDetector;
    private IInputHook? _inputHook;
    // [WARN] Référence forte au tray : sinon le GC collecte le NotifyIcon et l'icône disparaît (RG-T06).
    private TrayIconController? _tray;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // [DECISION] Axe 7 (lève [DT-012]) : l'app vit dans le tray. Fermer la dernière fenêtre ne quitte
        // plus le process (RG-T01) — elle est masquée. La sortie réelle passe par « Quitter » du menu tray,
        // qui appelle Application.Shutdown() (déclenche OnExit : unhook + flush autosave + Dispose tray).
        ShutdownMode = ShutdownMode.OnExplicitShutdown;

        // 1. Persistance : charger la config en tête (fichier absent/corrompu → défaut sans crash).
        IConfigStore configStore = new JsonConfigStore();
        var config = configStore.Load();

        // 2. Services : autosave débouncé + détecteur de fenêtres + registre démarrage + dialogues fichier.
        _autosave = new ConfigAutosaveService(configStore);
        _windowDetector = new WindowDetector();
        IStartupRegistryService startup = new StartupRegistryService();
        IFileDialogService fileDialog = new FileDialogService();
        // Cycle de session (Axe 9) : lancement du launcher / fermeture des clients (exception C-02, [DT-027]).
        ISessionProcessService session = new SessionProcessService();

        // Réconcilie le démarrage Windows avec l'intention persistée : re-pointe l'entrée Run vers l'exe
        // courant (chemin changé/réinstallation) ou la retire si l'option est off (RG-T05).
        startup.SetEnabled(config.StartWithWindows);

        // 3. ViewModel racine : reçoit la config, le détecteur et les services (capture, registre, dialogues) ;
        //    le VM Comptes s'abonne dès ici, avant Start, pour capter l'énumération initiale.
        IInputCaptureService inputCapture = new InputCaptureService();
        // Sortie réelle (« Terminer session », bouton « Fermer l'application ») : poser ForceClose AVANT
        // Shutdown() sinon MainWindow.OnClosing l'annule quand « fermer minimise » est actif ; Shutdown()
        // déclenche OnExit (unhook + flush + dispose tray). La fenêtre est capturée (assignée plus bas).
        Views.MainWindow? window = null;
        Action requestShutdown = () =>
        {
            if (window is not null) window.ForceClose = true;
            Current.Shutdown();
        };
        var mainViewModel = new MainViewModel(config, _windowDetector, inputCapture, startup, fileDialog,
            session, requestShutdown);
        mainViewModel.ConfigChanged += _autosave.Notify;

        // 4. Interception & bascule de focus (Axe 6) : décision pure → activation → coordinateur.
        //    Le coordinateur lit l'instantané de rotation du VM (sur le thread UI, dans le callback).
        IWindowActivator activator = new WindowActivator();
        ISwitchController switchController = new SwitchController();
        var coordinator = new SwitchCoordinator(switchController, activator, mainViewModel.BuildRotationSnapshot);
        coordinator.UpdateConfig(config);
        mainViewModel.ConfigChanged += coordinator.UpdateConfig; // set d'entrées + suspension à jour
        _inputHook = new InputHook(coordinator.Handle);

        // 5. Fenêtre principale + tray (référence forte gardée par App, archi §Composition root).
        window = new MainWindow { DataContext = mainViewModel };
        _tray = new TrayIconController(window, mainViewModel);
        window.Show();

        // 6. Détection puis hooks : démarrés après Show() pour que les callbacks OUTOFCONTEXT / bas niveau
        //    soient servis par la file de messages du thread UI déjà en pompe (archi §Threading, RG-D05/S06).
        _windowDetector.Start();
        _inputHook.Start();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        // Libérer les hooks natifs (souris/clavier puis WinEvent), le tray (icône, RG-T06) et flusher l'autosave.
        _inputHook?.Dispose();
        _windowDetector?.Dispose();
        _tray?.Dispose();
        _autosave?.Dispose();
        base.OnExit(e);
    }
}
