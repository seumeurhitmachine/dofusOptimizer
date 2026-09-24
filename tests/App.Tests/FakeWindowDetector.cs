using DofusSwitcher.Models;
using DofusSwitcher.Services;

namespace DofusSwitcher.Tests;

/// <summary>
/// Double de test pour <see cref="IWindowDetector"/> : permet de simuler apparition/disparition de
/// fenêtres sans dépendance Win32 ni thread UI (les events sont levés de façon synchrone).
/// </summary>
public sealed class FakeWindowDetector : IWindowDetector
{
    public event Action<DetectedWindow>? AccountAppeared;
    public event Action<DetectedWindow>? AccountDisappeared;

    public bool Started { get; private set; }

    public void Start() => Started = true;
    public void Stop() => Started = false;
    public void Dispose() => Stop();

    /// <summary>Simule l'apparition d'une fenêtre DOFUS.</summary>
    public void RaiseAppeared(string characterName, nint handle)
        => AccountAppeared?.Invoke(new DetectedWindow(characterName, handle));

    /// <summary>Simule la disparition d'une fenêtre DOFUS.</summary>
    public void RaiseDisappeared(string characterName, nint handle)
        => AccountDisappeared?.Invoke(new DetectedWindow(characterName, handle));
}
