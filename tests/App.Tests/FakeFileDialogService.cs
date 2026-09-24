using DofusSwitcher.Services;

namespace DofusSwitcher.Tests;

/// <summary>
/// Double de test pour <see cref="IFileDialogService"/> : retourne des chemins pré-programmés (ou
/// <c>null</c> pour simuler l'annulation), sans afficher de dialogue. Les chemins pointent typiquement
/// vers des fichiers temporaires dans les tests d'export/import.
/// </summary>
public sealed class FakeFileDialogService : IFileDialogService
{
    private readonly string? _savePath;
    private readonly string? _openPath;

    public FakeFileDialogService(string? savePath = null, string? openPath = null)
    {
        _savePath = savePath;
        _openPath = openPath;
    }

    /// <summary>Dernier nom de fichier suggéré passé à <see cref="AskSavePath"/>.</summary>
    public string? LastSuggestedName { get; private set; }

    public string? AskSavePath(string suggestedFileName)
    {
        LastSuggestedName = suggestedFileName;
        return _savePath;
    }

    public string? AskOpenPath() => _openPath;
}
