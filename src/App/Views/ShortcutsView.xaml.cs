using UserControl = System.Windows.Controls.UserControl;

namespace DofusSwitcher.Views;

/// <summary>
/// Vue de l'onglet Raccourcis. [ARCH] Code-behind réduit à <c>InitializeComponent</c> : capture, conflit
/// et persistance vivent dans <see cref="ViewModels.ShortcutsViewModel"/> (fourni par binding depuis MainWindow).
/// </summary>
public partial class ShortcutsView : UserControl
{
    public ShortcutsView() => InitializeComponent();
}
