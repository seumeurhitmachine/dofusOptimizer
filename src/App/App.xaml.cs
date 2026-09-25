using System.Threading;
using System.Windows;
using DofusSwitcher.Constants;
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
    // Instance unique (Axe 9) : le mutex détient l'unicité, l'événement réveille l'instance en cours.
    private Mutex? _instanceMutex;
    private EventWaitHandle? _showEvent;
    private RegisteredWaitHandle? _showRegistration;
    private MainWindow? _window;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // [DECISION] Axe 7 (lève [DT-012]) : l'app vit dans le tray. Fermer la dernière fenêtre ne quitte
        // plus le process (RG-T01) — elle est masquée. La sortie réelle passe par « Quitter » du menu tray,
        // qui appelle Application.Shutdown() (déclenche OnExit : unhook + flush autosave + Dispose tray).
        ShutdownMode = ShutdownMode.OnExplicitShutdown;

        // 0. Instance unique (Axe 9) : si l'app tourne déjà, réveiller sa fenêtre et quitter immédiatement
        //    (avant toute création de service/fenêtre/hook) plutôt que d'ouvrir un 2ᵉ process.
        _showEvent = new EventWaitHandle(false, EventResetMode.AutoReset, AppConstants.ShowWindowEventName);
        _instanceMutex = new Mutex(initiallyOwned: true, AppConstants.SingleInstanceMutexName, out var isFirstInstance);
        if (!isFirstInstance)
        {
            _showEvent.Set();   // demande à l'instance en cours d'afficher sa fenêtre
            Shutdown();         // OnExit libère les objets kernel (aucun autre service créé ici)
            return;
        }

        // Réveil déclenché par une 2ᵉ instance : afficher la fenêtre sur le thread UI (callback threadpool).
        _showRegistration = ThreadPool.RegisterWaitForSingleObject(
            _showEvent, (_, _) => Dispatcher.Invoke(ShowMainWindow), state: null, Timeout.Infinite, executeOnlyOnce: false);

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
        // déclenche OnExit (unhook + flush + dispose tray). La fenêtre (_window) est assignée plus bas.
        Action requestShutdown = () =>
        {
            if (_window is not null) _window.ForceClose = true;
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
        _window = new MainWindow { DataContext = mainViewModel };
        _tray = new TrayIconController(_window, mainViewModel);
        _window.Show();

        // 6. Détection puis hooks : démarrés après Show() pour que les callbacks OUTOFCONTEXT / bas niveau
        //    soient servis par la file de messages du thread UI déjà en pompe (archi §Threading, RG-D05/S06).
        _windowDetector.Start();
        _inputHook.Start();
    }

    /// <summary>Affiche et active la fenêtre principale (réveil par une 2ᵉ instance). Sur le thread UI.</summary>
    private void ShowMainWindow()
    {
        if (_window is null) return;
        _window.Show();
        _window.WindowState = WindowState.Normal;
        _window.Activate();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        // Libérer les hooks natifs (souris/clavier puis WinEvent), le tray (icône, RG-T06) et flusher l'autosave.
        _inputHook?.Dispose();
        _windowDetector?.Dispose();
        _tray?.Dispose();
        _autosave?.Dispose();
        // Instance unique : retirer l'attente puis libérer les objets kernel (le mutex relâche l'unicité).
        _showRegistration?.Unregister(waitObject: null);
        _showEvent?.Dispose();
        _instanceMutex?.Dispose();
        base.OnExit(e);
    }
}
