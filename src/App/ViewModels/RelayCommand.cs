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

/// <summary>
/// Variante paramétrée : reçoit le <c>CommandParameter</c> (typé <typeparamref name="T"/>) du binding.
/// Utilisée pour les actions par-ligne (ex. exclure/réintégrer un compte donné) où la cible n'est pas
/// la sélection courante mais l'item porté par le bouton.
/// </summary>
public sealed class RelayCommand<T>(Action<T> execute, Func<T, bool>? canExecute = null) : ICommand
{
    public event EventHandler? CanExecuteChanged;
    public bool CanExecute(object? parameter) => parameter is T value ? canExecute?.Invoke(value) ?? true : parameter is null && canExecute is null;
    public void Execute(object? parameter) { if (parameter is T value) execute(value); }
    public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
}
