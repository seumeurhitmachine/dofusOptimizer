using DofusSwitcher.Models;

// [WARN] App référence WinForms : Binding est ambigu. Alias explicite.
using Binding = DofusSwitcher.Models.Binding;

namespace DofusSwitcher.Services;

/// <summary>
/// Relie la capture bas niveau (<see cref="IInputHook"/>) à la décision (<see cref="ISwitchController"/>)
/// et à l'activation (<see cref="IWindowActivator"/>). Détient le pré-filtre (set des entrées associées)
/// et l'état de suspension, rafraîchis par <see cref="UpdateConfig"/> sur chaque changement de config.
/// [ARCH] Découplé du ViewModel : reçoit l'instantané de rotation via un délégué (invoqué sur le thread
/// UI, dans le callback du hook — lecture sûre de l'état runtime).
/// [WARN] <see cref="Handle"/> est le hot path du hook : cas courant (entrée non associée ou interception
/// suspendue) = deux tests O(1), aucun instantané construit (RG-S06).
/// </summary>
public sealed class SwitchCoordinator
{
    private readonly ISwitchController _controller;
    private readonly IWindowActivator _activator;
    private readonly Func<RotationSnapshot> _snapshotProvider;

    private HashSet<Binding> _bound = [];
    private bool _suspended;

    /// <summary>Câble le coordinateur ; <paramref name="snapshotProvider"/> est appelé sur le thread UI.</summary>
    public SwitchCoordinator(ISwitchController controller, IWindowActivator activator, Func<RotationSnapshot> snapshotProvider)
    {
        _controller = controller;
        _activator = activator;
        _snapshotProvider = snapshotProvider;
    }

    /// <summary>Recharge le pré-filtre (entrées associées) et l'état de suspension depuis la config.</summary>
    public void UpdateConfig(AppConfig config)
    {
        _bound = [.. config.AllBindings()];
        _suspended = config.InterceptionSuspended;
    }

    /// <summary>
    /// Gestionnaire d'entrée branché sur le hook. Retourne <c>true</c> si l'entrée est consommée.
    /// Pré-filtre O(1) puis, seulement si l'entrée est associée et l'interception active, construit
    /// l'instantané, décide et active la fenêtre cible.
    /// </summary>
    public bool Handle(Binding input)
    {
        if (_suspended || !_bound.Contains(input)) return false; // RG-S02 / entrée libre → natif

        var decision = _controller.Decide(_snapshotProvider(), input, _activator.GetForeground());
        if (!decision.Consume) return false;

        if (decision.TargetHandle != 0)
            _activator.Activate(decision.TargetHandle);
        return true; // consommée (un seul changement de focus, C-01)
    }
}
