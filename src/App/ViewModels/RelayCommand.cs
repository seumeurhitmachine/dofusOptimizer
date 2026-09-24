using System.Windows.Input;

namespace DofusSwitcher.ViewModels;

/// <summary>ICommand synchrone maison. Pour l'async, encapsuler l'appel et gérer l'état soi-même.</summary>
public sealed class RelayCommand(Action execute, Func<bool>? canExecute = null) : ICommand
{
    public event EventHandler? CanExecuteChanged;
    public bool CanExecute(object? _) => canExecute?.Invoke() ?? true;
    public void Execute(object? _) => execute();
    public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
}
