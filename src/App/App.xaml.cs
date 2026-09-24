using System.Windows;
using DofusSwitcher.Persistence;
using DofusSwitcher.Services;
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

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // [DECISION] Tant que le tray n'existe pas (Axe 7), fermer la fenêtre DOIT quitter le process :
        // sinon l'app survit sans UI (process fantôme) et OnExit (unhook WinEvent + flush autosave) ne
        // s'exécute jamais. L'Axe 7 rétablira OnExplicitShutdown + « fermer = masquer dans le tray ».
        ShutdownMode = ShutdownMode.OnLastWindowClose;

        // 1. Persistance : charger la config en tête (fichier absent/corrompu → défaut sans crash).
        IConfigStore configStore = new JsonConfigStore();
        var config = configStore.Load();

        // 2. Services : autosave débouncé + détecteur de fenêtres (instanciés sur le thread UI).
        _autosave = new ConfigAutosaveService(configStore);
        _windowDetector = new WindowDetector();

        // 3. ViewModel racine : reçoit la config, le détecteur et le service de capture (modale WPF) ;
        //    le VM Comptes s'abonne dès ici, avant Start, pour capter l'énumération initiale.
        IInputCaptureService inputCapture = new InputCaptureService();
        var mainViewModel = new MainViewModel(config, _windowDetector, inputCapture);
        mainViewModel.ConfigChanged += _autosave.Notify;

        // 4. Fenêtre principale.
        var window = new MainWindow { DataContext = mainViewModel };
        window.Show();

        // 5. Détection : démarrée après Show() pour que le hook OUTOFCONTEXT poste ses événements
        //    dans la file de messages du thread UI déjà en pompe (archi §Threading, RG-D05).
        _windowDetector.Start();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        // Libérer le hook natif puis flusher un éventuel instantané en attente avant de quitter.
        _windowDetector?.Dispose();
        _autosave?.Dispose();
        base.OnExit(e);
    }
}
