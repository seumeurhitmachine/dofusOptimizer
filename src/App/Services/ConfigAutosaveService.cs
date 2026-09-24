using System.Windows.Threading;
using DofusSwitcher.Constants;
using DofusSwitcher.Models;
using DofusSwitcher.Persistence;

namespace DofusSwitcher.Services;

/// <summary>
/// Débounce l'autosave via un <see cref="DispatcherTimer"/> court réarmé à chaque modification.
/// [DECISION] Le débounce vit dans ce service dédié, pas dans le ViewModel racine : celui-ci reste
/// libre de tout type WPF (timer) et de toute logique de threading, conforme au découpage en
/// couches (archi §Découpage, §Threading). Le ViewModel notifie via <see cref="IConfigAutosave"/>.
/// [WARN] Le <see cref="DispatcherTimer"/> s'exécute sur le thread qui l'a créé : instancier ce
/// service depuis la composition root (thread UI) pour que le tick reste sur le Dispatcher.
/// </summary>
public sealed class ConfigAutosaveService : IConfigAutosave, IDisposable
{
    private readonly IConfigStore _store;
    private readonly DispatcherTimer _timer;
    private AppConfig? _pending;

    /// <summary>Crée le service d'autosave adossé au <paramref name="store"/> fourni.</summary>
    public ConfigAutosaveService(IConfigStore store)
    {
        _store = store;
        _timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(AppConstants.AutosaveDebounceMs) };
        _timer.Tick += OnTick;
    }

    /// <inheritdoc/>
    public void Notify(AppConfig config)
    {
        _pending = config;
        // Réarme : Stop puis Start remet le compte à rebours à zéro à chaque modification.
        _timer.Stop();
        _timer.Start();
    }

    /// <inheritdoc/>
    public void SaveNow()
    {
        _timer.Stop();
        Flush();
    }

    private void OnTick(object? sender, EventArgs e)
    {
        _timer.Stop();
        Flush();
    }

    private void Flush()
    {
        if (_pending is null) return;
        _store.Save(_pending);
        _pending = null;
    }

    /// <summary>Arrête le timer et écrit un éventuel instantané en attente.</summary>
    public void Dispose()
    {
        _timer.Tick -= OnTick;
        SaveNow();
    }
}
