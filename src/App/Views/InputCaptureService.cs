using DofusSwitcher.Services;

// [WARN] Alias explicites vers WPF + notre Binding (ambiguïté WinForms).
using Application = System.Windows.Application;
using Binding = DofusSwitcher.Models.Binding;

namespace DofusSwitcher.Views;

/// <summary>
/// Implémentation WPF de <see cref="IInputCaptureService"/> : montre <see cref="CaptureInputWindow"/> en
/// modale, possédée par la fenêtre principale (centrage + modalité). Vit côté Views car elle instancie
/// une fenêtre — les ViewModels n'en dépendent que via l'interface (règle MVVM).
/// </summary>
public sealed class InputCaptureService : IInputCaptureService
{
    /// <inheritdoc />
    public Binding? Capture(string actionLabel)
    {
        var window = new CaptureInputWindow(actionLabel) { Owner = Application.Current?.MainWindow };
        return window.ShowDialog() == true ? window.Result : null;
    }
}
