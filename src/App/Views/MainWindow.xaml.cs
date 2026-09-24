using System.ComponentModel;
using System.Windows;

namespace DofusSwitcher.Views;

/// <summary>
/// Fenêtre principale — shell à onglets.
/// [ARCH] Code-behind minimal : InitializeComponent + câblage du cycle de vie fenêtre ↔ tray (autorisé par
/// l'archi §MVVM). Aucune logique métier ici (elle vit dans les ViewModels).
/// </summary>
public partial class MainWindow : Window
{
    /// <summary>
    /// Quand vrai, la fermeture ferme réellement la fenêtre (sortie de l'app via « Quitter » du tray).
    /// Sinon, fermer la fenêtre la masque : l'app continue de vivre dans le tray (RG-T01).
    /// </summary>
    public bool ForceClose { get; set; }

    public MainWindow()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Fermer la fenêtre la masque au lieu de quitter (RG-T01). La sortie réelle passe par le menu du tray,
    /// qui pose <see cref="ForceClose"/> puis appelle <c>Application.Shutdown()</c>.
    /// </summary>
    protected override void OnClosing(CancelEventArgs e)
    {
        if (!ForceClose)
        {
            e.Cancel = true;
            Hide();
        }
        base.OnClosing(e);
    }
}
