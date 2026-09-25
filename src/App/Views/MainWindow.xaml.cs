using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
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

    private SettingsViewModel? Settings => (DataContext as MainViewModel)?.Settings;

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

    /// <summary>Minimiser [_] : masque dans la barre d'état si l'option est active, sinon minimisation classique.</summary>
    protected override void OnStateChanged(EventArgs e)
    {
        if (WindowState == WindowState.Minimized && Settings?.MinimizeToTray == true)
            Hide(); // retiré de la barre des tâches ; le tray reste le point d'accès
        base.OnStateChanged(e);
    }

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
