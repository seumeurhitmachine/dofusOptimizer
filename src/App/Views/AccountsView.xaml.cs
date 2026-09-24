// [WARN] UseWPF + UseWindowsForms exposent deux UserControl : lever l'ambiguïté en faveur de WPF.
using UserControl = System.Windows.Controls.UserControl;

namespace DofusSwitcher.Views;

/// <summary>
/// Vue de l'onglet Comptes.
/// [ARCH] Code-behind minimal : InitializeComponent uniquement. Le DataContext (AccountsViewModel)
/// est fourni par le binding depuis MainWindow (archi §MVVM).
/// </summary>
public partial class AccountsView : UserControl
{
    public AccountsView()
    {
        InitializeComponent();
    }
}
