using DofusSwitcher.Constants;
using DofusSwitcher.Models;
using DofusSwitcher.Services;

// [WARN] App référence WinForms : Binding est ambigu. Alias explicite.
using Binding = DofusSwitcher.Models.Binding;

namespace DofusSwitcher.Tests;

/// <summary>
/// Tests du coordinateur d'interception (<see cref="SwitchCoordinator"/>) avec doubles : pré-filtre des
/// entrées associées, suspension (RG-S02) et acheminement décision → activation. Aucun interop réel.
/// </summary>
public class SwitchCoordinatorTests
{
    private static readonly Binding Next = new(BindingKind.MouseButton, AppConstants.DefaultNextButtonCode);
    private static readonly Binding Libre = new(BindingKind.Key, 0x41); // A, non associée

    private sealed class FakeActivator : IWindowActivator
    {
        public nint Foreground { get; set; }
        public nint Activated { get; private set; }
        public int ActivateCalls { get; private set; }
        public nint GetForeground() => Foreground;
        public void Activate(nint handle) { Activated = handle; ActivateCalls++; }
    }

    // Contrôleur factice : renvoie une décision fixe, et compte les appels (preuve du pré-filtre).
    private sealed class FakeController(SwitchDecision decision) : ISwitchController
    {
        public int DecideCalls { get; private set; }
        public SwitchDecision Decide(RotationSnapshot snapshot, Binding input, nint foreground)
        {
            DecideCalls++;
            return decision;
        }
    }

    private static RotationSnapshot EmptySnapshot()
        => new([], Next, null, new Dictionary<Binding, nint>());

    private static AppConfig ConfigWithNext(bool suspended = false)
        => AppConfig.Default with { PrevBinding = null, InterceptionSuspended = suspended };

    [Fact]
    public void EntreeAssociee_SurDofus_DecideEtActive()
    {
        var activator = new FakeActivator { Foreground = 1 };
        var controller = new FakeController(SwitchDecision.Handle(2));
        var coordinator = new SwitchCoordinator(controller, activator, EmptySnapshot);
        coordinator.UpdateConfig(ConfigWithNext());

        var consumed = coordinator.Handle(Next);

        Assert.True(consumed);
        Assert.Equal(1, controller.DecideCalls);
        Assert.Equal(2, activator.Activated);
    }

    [Fact]
    public void EntreeNonAssociee_IgnoreeSansDecider() // pré-filtre O(1)
    {
        var controller = new FakeController(SwitchDecision.Handle(2));
        var coordinator = new SwitchCoordinator(controller, new FakeActivator(), EmptySnapshot);
        coordinator.UpdateConfig(ConfigWithNext());

        Assert.False(coordinator.Handle(Libre));
        Assert.Equal(0, controller.DecideCalls); // aucun instantané/décision construit
    }

    [Fact]
    public void InterceptionSuspendue_LaissePasser() // RG-S02
    {
        var controller = new FakeController(SwitchDecision.Handle(2));
        var coordinator = new SwitchCoordinator(controller, new FakeActivator(), EmptySnapshot);
        coordinator.UpdateConfig(ConfigWithNext(suspended: true));

        Assert.False(coordinator.Handle(Next));
        Assert.Equal(0, controller.DecideCalls);
    }

    [Fact]
    public void DecisionPass_NeConsommePas_EtNActivePas()
    {
        var activator = new FakeActivator { Foreground = 999 };
        var controller = new FakeController(SwitchDecision.Pass);
        var coordinator = new SwitchCoordinator(controller, activator, EmptySnapshot);
        coordinator.UpdateConfig(ConfigWithNext());

        Assert.False(coordinator.Handle(Next));
        Assert.Equal(0, activator.ActivateCalls);
    }

    [Fact]
    public void ConsommeSansCible_NActivePas()
    {
        var activator = new FakeActivator { Foreground = 1 };
        var controller = new FakeController(SwitchDecision.Handle(0)); // consommée, aucune cible
        var coordinator = new SwitchCoordinator(controller, activator, EmptySnapshot);
        coordinator.UpdateConfig(ConfigWithNext());

        Assert.True(coordinator.Handle(Next));
        Assert.Equal(0, activator.ActivateCalls);
    }
}
