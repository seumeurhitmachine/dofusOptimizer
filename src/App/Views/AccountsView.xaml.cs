using DofusSwitcher.ViewModels;

// [WARN] UseWPF + UseWindowsForms exposent des types homonymes (UserControl, Point, MouseEventArgs,
// DragEventArgs, DragDrop…). Alias explicites en faveur de WPF pour tout ce fichier.
using UserControl = System.Windows.Controls.UserControl;
using ListBoxItem = System.Windows.Controls.ListBoxItem;
using FrameworkElement = System.Windows.FrameworkElement;
using DependencyObject = System.Windows.DependencyObject;
using Point = System.Windows.Point;
using MouseButtonEventArgs = System.Windows.Input.MouseButtonEventArgs;
using MouseEventArgs = System.Windows.Input.MouseEventArgs;
using MouseButtonState = System.Windows.Input.MouseButtonState;
using DragEventArgs = System.Windows.DragEventArgs;
using DragDrop = System.Windows.DragDrop;
using DragDropEffects = System.Windows.DragDropEffects;
using SystemParameters = System.Windows.SystemParameters;
using VisualTreeHelper = System.Windows.Media.VisualTreeHelper;

namespace DofusSwitcher.Views;

/// <summary>
/// Vue de l'onglet Comptes.
/// [ARCH] Code-behind volontairement limité au glisser-déposer : c'est un concern purement vue (hit-
/// test visuel) qui n'a pas sa place dans le ViewModel. Le réordonnancement effectif passe par
/// <see cref="AccountsViewModel.MoveItem"/> — chemin unique de mutation (archi §MVVM). Le DataContext
/// (AccountsViewModel) est fourni par le binding depuis MainWindow.
/// </summary>
public partial class AccountsView : UserControl
{
    private Point _dragStartPoint;
    private AccountItemViewModel? _draggedItem;

    public AccountsView()
    {
        InitializeComponent();
    }

    private void Handle_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        _dragStartPoint = e.GetPosition(null);
        _draggedItem = (sender as FrameworkElement)?.DataContext as AccountItemViewModel;
    }

    private void Handle_PreviewMouseMove(object sender, MouseEventArgs e)
    {
        if (e.LeftButton != MouseButtonState.Pressed || _draggedItem is null) return;

        // Ne démarrer le drag qu'au-delà du seuil système (évite les faux déplacements au clic).
        var diff = _dragStartPoint - e.GetPosition(null);
        if (Math.Abs(diff.X) < SystemParameters.MinimumHorizontalDragDistance &&
            Math.Abs(diff.Y) < SystemParameters.MinimumVerticalDragDistance)
            return;

        if (sender is FrameworkElement element)
            DragDrop.DoDragDrop(element, _draggedItem, DragDropEffects.Move);

        _draggedItem = null;
    }

    private void AccountsList_Drop(object sender, DragEventArgs e)
    {
        if (_draggedItem is null || DataContext is not AccountsViewModel vm) return;

        var from = vm.Items.IndexOf(_draggedItem);

        // Cible = compte survolé au lâcher ; à défaut (zone vide sous la liste), fin de liste.
        var target = FindAncestor<ListBoxItem>(AccountsList.InputHitTest(e.GetPosition(AccountsList)) as DependencyObject);
        var to = target?.DataContext is AccountItemViewModel dropOn
            ? vm.Items.IndexOf(dropOn)
            : vm.Items.Count - 1;

        if (from >= 0 && to >= 0) vm.MoveItem(from, to);
        _draggedItem = null;
    }

    private static T? FindAncestor<T>(DependencyObject? current) where T : DependencyObject
    {
        while (current is not null)
        {
            if (current is T match) return match;
            current = VisualTreeHelper.GetParent(current);
        }
        return null;
    }
}
