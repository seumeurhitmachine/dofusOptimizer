using DofusSwitcher.Models;
using DofusSwitcher.Services;

// [WARN] App référence WinForms : Binding est ambigu. Alias explicite.
using Binding = DofusSwitcher.Models.Binding;

namespace DofusSwitcher.Tests;

/// <summary>
/// Double de test pour <see cref="IInputCaptureService"/> : retourne une entrée pré-programmée (ou
/// <c>null</c> pour simuler l'annulation par Échap), sans afficher de fenêtre.
/// </summary>
public sealed class FakeInputCaptureService : IInputCaptureService
{
    private readonly Binding? _toReturn;

    public FakeInputCaptureService(Binding? toReturn) => _toReturn = toReturn;

    /// <summary>Nombre d'ouvertures de la modale (capture demandée).</summary>
    public int CaptureCount { get; private set; }

    /// <summary>Dernier libellé d'action passé à la capture.</summary>
    public string? LastLabel { get; private set; }

    public Binding? Capture(string actionLabel)
    {
        CaptureCount++;
        LastLabel = actionLabel;
        return _toReturn;
    }
}
