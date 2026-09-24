using DofusSwitcher.Constants;
using DofusSwitcher.Services;

// [WARN] UseWPF + UseWindowsForms : *FileDialog est ambigu. Alias vers les dialogues WPF (Microsoft.Win32).
using SaveFileDialog = Microsoft.Win32.SaveFileDialog;
using OpenFileDialog = Microsoft.Win32.OpenFileDialog;

namespace DofusSwitcher.Views;

/// <summary>
/// Implémentation WPF de <see cref="IFileDialogService"/> via <c>Microsoft.Win32.SaveFileDialog</c>/
/// <c>OpenFileDialog</c> (in-box, aucun NuGet). Vit côté Views (seul endroit autorisé à toucher WPF, ref
/// [DT-014]) ; les ViewModels ne dépendent que de l'interface.
/// </summary>
public sealed class FileDialogService : IFileDialogService
{
    /// <inheritdoc/>
    public string? AskSavePath(string suggestedFileName)
    {
        var dialog = new SaveFileDialog
        {
            Filter = AppConstants.ConfigFileDialogFilter,
            FileName = suggestedFileName,
            DefaultExt = ".json",
            AddExtension = true,
        };
        return dialog.ShowDialog() == true ? dialog.FileName : null;
    }

    /// <inheritdoc/>
    public string? AskOpenPath()
    {
        var dialog = new OpenFileDialog
        {
            Filter = AppConstants.ConfigFileDialogFilter,
            CheckFileExists = true,
        };
        return dialog.ShowDialog() == true ? dialog.FileName : null;
    }
}
