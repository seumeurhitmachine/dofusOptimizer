using DofusSwitcher.Services;

namespace DofusSwitcher.Tests;

/// <summary>
/// Double de test pour <see cref="IStartupRegistryService"/> : mémorise l'état demandé sans toucher au
/// registre réel. Permet d'asserter que l'onglet Réglages pose/retire bien le démarrage automatique.
/// </summary>
public sealed class FakeStartupRegistryService : IStartupRegistryService
{
    private bool _enabled;

    public FakeStartupRegistryService(bool initiallyEnabled = false) => _enabled = initiallyEnabled;

    /// <summary>Nombre d'appels à <see cref="SetEnabled"/>.</summary>
    public int SetCount { get; private set; }

    public bool IsEnabled() => _enabled;

    public void SetEnabled(bool enabled)
    {
        SetCount++;
        _enabled = enabled;
    }
}
