using DofusSwitcher.ViewModels;

// [WARN] UseWPF + UseWindowsForms exposent des types homonymes. Alias explicites en faveur de WPF.
using UserControl = System.Windows.Controls.UserControl;
using FrameworkElement = System.Windows.FrameworkElement;
using Point = System.Windows.Point;
using MouseButtonEventArgs = System.Windows.Input.MouseButtonEventArgs;
using MouseEventArgs = System.Windows.Input.MouseEventArgs;
using MouseButtonState = System.Windows.Input.MouseButtonState;
using DragEventArgs = System.Windows.DragEventArgs;
using DragDrop = System.Windows.DragDrop;
using DragDropEffects = System.Windows.DragDropEffects;
using SystemParameters = System.Windows.SystemParameters;

namespace DofusSwitcher.Views;

/// <summary>
/// Vue de l'onglet Comptes (v2) : 3 zones pilotées par databinding. Le seul code-behind est le
/// glisser-déposer de réordonnancement de la <b>zone 1</b> (comptes connectés) — concern purement vue
/// (hit-test), qui réordonne l'ordre de rotation via <see cref="AccountsViewModel.MoveItem"/> (chemin
/// unique de mutation). La position est persistée même pour les personnages absents.
/// </summary>
public partial class AccountsView : UserControl
{
    private Point _dragStart;
    private AccountItemViewModel? _dragged;

    public AccountsView()
    {
        InitializeComponent();
    }

    private void Handle_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        _dragStart = e.GetPosition(null);
        _dragged = ((sender as FrameworkElement)?.DataContext as ConnectedAccountViewModel)?.Character;
    }

    private void Handle_PreviewMouseMove(object sender, MouseEventArgs e)
    {
        if (e.LeftButton != MouseButtonState.Pressed || _dragged is null) return;

        // Ne démarrer le drag qu'au-delà du seuil système (évite les faux déplacements au clic).
        var diff = _dragStart - e.GetPosition(null);
        if (Math.Abs(diff.X) < SystemParameters.MinimumHorizontalDragDistance &&
            Math.Abs(diff.Y) < SystemParameters.MinimumVerticalDragDistance)
            return;

        if (sender is FrameworkElement element)
            DragDrop.DoDragDrop(element, _dragged, DragDropEffects.Move);

        _dragged = null;
    }

    private void Row_Drop(object sender, DragEventArgs e)
    {
        if (_dragged is null || DataContext is not AccountsViewModel vm) return;
        if ((sender as FrameworkElement)?.DataContext is not ConnectedAccountViewModel target) return;

        var from = vm.Items.IndexOf(_dragged);
        var to = vm.Items.IndexOf(target.Character);
        if (from >= 0 && to >= 0) vm.MoveItem(from, to);
        _dragged = null;
    }
}
