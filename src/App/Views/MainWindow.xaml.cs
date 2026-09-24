using System.Windows;

namespace DofusSwitcher.Views;

/// <summary>
/// Fenêtre principale — shell à onglets.
/// [ARCH] Code-behind minimal : InitializeComponent uniquement. Aucune logique métier ici
/// (elle vit dans les ViewModels). Le câblage du cycle de vie fenêtre ↔ tray arrive à l'Axe 7.
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }
}
