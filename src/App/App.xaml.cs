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

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // [ARCH] ShutdownMode manuel : à terme l'app vit dans le tray (Axe 7), fermer la
        // fenêtre ne doit pas quitter le process. Posé dès maintenant pour cohérence.
        ShutdownMode = ShutdownMode.OnExplicitShutdown;

        // 1. Persistance : charger la config en tête (fichier absent/corrompu → défaut sans crash).
        IConfigStore configStore = new JsonConfigStore();
        var config = configStore.Load();

        // 2. Services : autosave débouncé adossé au store (timer créé sur le thread UI).
        _autosave = new ConfigAutosaveService(configStore);

        // 3. ViewModel racine : reçoit la config ; ses modifications futures alimentent l'autosave.
        var mainViewModel = new MainViewModel(config);
        mainViewModel.ConfigChanged += _autosave.Notify;

        // 4. Fenêtre principale.
        var window = new MainWindow { DataContext = mainViewModel };
        window.Show();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        // Flush d'un éventuel instantané en attente avant de quitter, puis arrêt du timer.
        _autosave?.Dispose();
        base.OnExit(e);
    }
}
