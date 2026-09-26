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
/// Vue de l'onglet Comptes (Axe 10) : liste unifiée des connectés + comptes déconnectés, pilotées par
/// databinding. Le seul code-behind est le glisser-déposer de réordonnancement de la <b>liste des
/// connectés</b> (toute ligne, liée ou non) — concern purement vue (hit-test). Le glisser prévisualise
/// en direct : la ligne glissée devient « fantôme » et se réinsère
/// entre les autres (qui se décalent) via <see cref="AccountsViewModel.PreviewReorder"/> ; l'ordre n'est
/// figé et persisté qu'au lâcher (<see cref="AccountsViewModel.CommitReorder"/>), un glisser annulé étant
/// rétabli par <see cref="AccountsViewModel.CancelReorder"/>. La position est persistée même pour les
/// personnages absents.
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
        _dragged = ((sender as FrameworkElement)?.DataContext as ConnectedRowViewModel)?.Character;
    }

    private void Handle_PreviewMouseMove(object sender, MouseEventArgs e)
    {
        if (e.LeftButton != MouseButtonState.Pressed || _dragged is null) return;

        // Ne démarrer le drag qu'au-delà du seuil système (évite les faux déplacements au clic).
        var diff = _dragStart - e.GetPosition(null);
        if (Math.Abs(diff.X) < SystemParameters.MinimumHorizontalDragDistance &&
            Math.Abs(diff.Y) < SystemParameters.MinimumVerticalDragDistance)
            return;

        if (sender is not FrameworkElement element) return;

        // Rendu « fantôme » de la ligne glissée pendant toute l'opération modale (DoDragDrop bloque et pompe
        // les messages : DragOver/Drop s'exécutent dedans). Capturé en local car _dragged est mis à null au drop.
        var dragged = _dragged;
        dragged.IsDragging = true;
        var effect = DragDrop.DoDragDrop(element, dragged, DragDropEffects.Move);
        dragged.IsDragging = false;

        // Glisser annulé (Échap, lâcher hors cible) : rétablir l'ordre prévisualisé.
        if (effect != DragDropEffects.Move && DataContext is AccountsViewModel vm) vm.CancelReorder();
        _dragged = null;
    }

    private void Row_DragOver(object sender, DragEventArgs e)
    {
        if (_dragged is null || DataContext is not AccountsViewModel vm) return;
        if (sender is not FrameworkElement row || row.DataContext is not ConnectedRowViewModel target) return;
        // Les clients sans personnage (Axe 11) ne sont pas réordonnables (ordre non persisté) : ignorer.
        if (target.IsAnonymous) return;

        e.Effects = DragDropEffects.Move;
        e.Handled = true;
        // Insertion avant/après selon la moitié survolée : franchir le milieu réordonne, sans osciller.
        var insertAfter = e.GetPosition(row).Y > row.ActualHeight / 2;
        vm.PreviewReorder(_dragged, target.Character, insertAfter);
    }

    private void Row_Drop(object sender, DragEventArgs e)
    {
        if (DataContext is AccountsViewModel vm) vm.CommitReorder();
    }
}
