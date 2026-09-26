using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using DofusSwitcher.Interop;
using DofusSwitcher.ViewModels;

// [WARN] UseWPF + UseWindowsForms exposent deux Application/TabControl. Lever l'ambiguïté vers WPF.
using Application = System.Windows.Application;
using TabControl = System.Windows.Controls.TabControl;

namespace DofusSwitcher.Views;

/// <summary>
/// Fenêtre principale — shell à onglets.
/// [ARCH] Code-behind minimal : InitializeComponent + câblage du cycle de vie fenêtre ↔ tray/réglages
/// (autorisé par l'archi §MVVM). Aucune logique métier ici (elle vit dans les ViewModels).
/// </summary>
public partial class MainWindow : Window
{
    /// <summary>
    /// Quand vrai, la fermeture ferme réellement la fenêtre (sortie de l'app via « Quitter » du tray ou le
    /// bouton « Fermer l'application »). [WARN] Toute sortie réelle DOIT poser ce drapeau avant
    /// <c>Shutdown()</c> : sinon <see cref="OnClosing"/> annulerait la fermeture quand « fermer minimise » est actif.
    /// </summary>
    public bool ForceClose { get; set; }

    public MainWindow()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Coins légèrement arrondis (Axe 11) : dès que le handle natif existe, on demande la préférence de coins
    /// DWM (Windows 11 ; ignoré silencieusement sous Windows 10). Attribut d'affichage sur NOTRE fenêtre —
    /// aucun lien avec C-02/C-03. [WARN] À faire au SourceInitialized : le HWND n'existe pas avant.
    /// </summary>
    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);
        var hwnd = new WindowInteropHelper(this).Handle;
        var preference = NativeMethods.DWMWCP_ROUNDSMALL;
        _ = NativeMethods.DwmSetWindowAttribute(hwnd, NativeMethods.DWMWA_WINDOW_CORNER_PREFERENCE, ref preference, sizeof(int));
    }

    private SettingsViewModel? Settings => (DataContext as MainViewModel)?.Settings;

    /// <summary>
    /// Minimise l'application (Axe 11, « Réduire à l'ouverture d'une session ») : masque dans la barre d'état si
    /// l'option <c>MinimizeToTray</c> est active, sinon minimisation classique. Appelé via le rappel injecté au
    /// <see cref="MainViewModel"/> par la composition root.
    /// </summary>
    public void MinimizeApp() => MinimizeWindow(Settings?.MinimizeToTray ?? false);

    /// <summary>
    /// Fermer la fenêtre [X] : selon les réglages (Axe 9), minimise l'application (barre d'état ou barre des
    /// tâches) ou la quitte réellement. La sortie explicite (tray/bouton) passe par <see cref="ForceClose"/>.
    /// </summary>
    protected override void OnClosing(CancelEventArgs e)
    {
        if (ForceClose) { base.OnClosing(e); return; }

        var settings = Settings;
        if (settings is null || settings.CloseMinimizes)
        {
            e.Cancel = true;
            MinimizeWindow(settings?.MinimizeToTray ?? true);
        }
        else
        {
            // [X] quitte réellement : annuler cette fermeture et demander l'arrêt explicite (teardown propre).
            e.Cancel = true;
            ForceClose = true;
            Application.Current.Shutdown();
        }
        base.OnClosing(e);
    }

    /// <summary>Minimiser [_] : masque dans la barre d'état si l'option est active, sinon minimisation classique.
    /// Reflète aussi l'état agrandi/restauré sur le glyphe du bouton du bandeau personnalisé.</summary>
    protected override void OnStateChanged(EventArgs e)
    {
        if (WindowState == WindowState.Minimized && Settings?.MinimizeToTray == true)
            Hide(); // retiré de la barre des tâches ; le tray reste le point d'accès
        // Icône vectorielle : agrandir (carré) ↔ restaurer (double carré).
        var maximized = WindowState == WindowState.Maximized;
        MaximizeIcon.Data = (System.Windows.Media.Geometry)FindResource(
            maximized ? "CaptionRestoreGeometry" : "CaptionMaximizeGeometry");
        MaximizeButton.ToolTip = maximized ? "Restaurer" : "Agrandir";
        base.OnStateChanged(e);
    }

    /// <summary>Bouton « réduire » du bandeau personnalisé.</summary>
    private void MinimizeButton_Click(object sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;

    /// <summary>Bouton « agrandir / restaurer » du bandeau personnalisé.</summary>
    private void MaximizeButton_Click(object sender, RoutedEventArgs e) =>
        WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;

    /// <summary>Bouton « fermer » du bandeau : passe par <see cref="OnClosing"/> (respecte « fermer minimise »).</summary>
    private void CloseButton_Click(object sender, RoutedEventArgs e) => Close();

    private void MinimizeWindow(bool toTray)
    {
        if (toTray) Hide();
        else WindowState = WindowState.Minimized;
    }

    /// <summary>
    /// Changement d'onglet : replie la ligne d'ajout de compte dès qu'on quitte l'onglet Réglages (Axe 9).
    /// [WARN] SelectionChanged bulle depuis les contrôles internes — ne traiter que l'événement du TabControl.
    /// </summary>
    private void OnTabChanged(object sender, SelectionChangedEventArgs e)
    {
        if (e.Source is TabControl tab && !ReferenceEquals(tab.SelectedItem, ReglagesTab))
            Settings?.CollapseAddAccount();
    }
}
