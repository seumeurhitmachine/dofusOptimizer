// [WARN] UseWPF + UseWindowsForms : UserControl est ambigu. Alias explicite vers WPF.
using UserControl = System.Windows.Controls.UserControl;

namespace DofusSwitcher.Views;

/// <summary>
/// Vue de l'onglet Réglages (Axe 7).
/// [ARCH] Code-behind minimal : InitializeComponent uniquement. Toute la logique (suspension, démarrage
/// Windows, export/import) vit dans <see cref="ViewModels.SettingsViewModel"/> (DataContext).
/// </summary>
public partial class ReglagesView : UserControl
{
    public ReglagesView()
    {
        InitializeComponent();
    }
}
