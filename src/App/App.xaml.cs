using System.Windows;
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
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // [ARCH] ShutdownMode manuel : à terme l'app vit dans le tray (Axe 7), fermer la
        // fenêtre ne doit pas quitter le process. Posé dès maintenant pour cohérence.
        ShutdownMode = ShutdownMode.OnExplicitShutdown;

        // Axe 1 : socle uniquement. Persistance / services / tray arrivent aux Axes suivants.
        var mainViewModel = new MainViewModel();
        var window = new MainWindow { DataContext = mainViewModel };
        window.Show();
    }
}
