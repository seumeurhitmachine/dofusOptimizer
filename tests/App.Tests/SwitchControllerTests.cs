using DofusSwitcher.Constants;
using DofusSwitcher.Models;
using DofusSwitcher.Services;

// [WARN] App référence WinForms : Binding est ambigu. Alias explicite.
using Binding = DofusSwitcher.Models.Binding;

namespace DofusSwitcher.Tests;

/// <summary>
/// Tests purs de la décision d'interception (<see cref="SwitchController"/>, algo <c>rotation.md</c>) :
/// bascule cyclique avec saut des absents/exclus (CA-01/CA-03), interception conditionnée au focus DOFUS
/// (CA-02, C-01) et activation directe (US-S02). Aucun interop.
/// </summary>
public class SwitchControllerTests
{
    private static readonly Binding Next = new(BindingKind.MouseButton, AppConstants.DefaultNextButtonCode);
    private static readonly Binding Prev = new(BindingKind.MouseButton, AppConstants.DefaultPrevButtonCode);

    private static RotationSlot Connected(string name, nint handle, bool excluded = false)
        => new(name, handle, IsConnected: true, IsExcluded: excluded);

    private static RotationSlot Absent(string name)
        => new(name, 0, IsConnected: false, IsExcluded: false);

    private static RotationSnapshot Snapshot(IReadOnlyList<RotationSlot> slots, IReadOnlyDictionary<Binding, nint>? directs = null)
        => new(slots, Next, Prev, directs ?? new Dictionary<Binding, nint>());

    private readonly SwitchController _controller = new();

    [Fact]
    public void Suivant_ParcourtLes4ComptesCycliquement_RetourAuPremier() // CA-01
    {
        var slots = new[] { Connected("A", 1), Connected("B", 2), Connected("C", 3), Connected("D", 4) };
        var snap = Snapshot(slots);

        Assert.Equal(2, _controller.Decide(snap, Next, foreground: 1).TargetHandle);
        Assert.Equal(3, _controller.Decide(snap, Next, foreground: 2).TargetHandle);
        Assert.Equal(4, _controller.Decide(snap, Next, foreground: 3).TargetHandle);
        Assert.Equal(1, _controller.Decide(snap, Next, foreground: 4).TargetHandle); // cyclique
    }

    [Fact]
    public void Precedent_EstSymetrique()
    {
        var slots = new[] { Connected("A", 1), Connected("B", 2), Connected("C", 3) };
        var snap = Snapshot(slots);

        Assert.Equal(3, _controller.Decide(snap, Prev, foreground: 1).TargetHandle); // cyclique en arrière
        Assert.Equal(1, _controller.Decide(snap, Prev, foreground: 2).TargetHandle);
    }

    [Fact]
    public void Suivant_IgnoreUnCompteAbsentAuMilieu() // CA-03
    {
        var slots = new[] { Connected("A", 1), Absent("B"), Connected("C", 3) };
        var decision = _controller.Decide(Snapshot(slots), Next, foreground: 1);

        Assert.True(decision.Consume);
        Assert.Equal(3, decision.TargetHandle); // B (absent) sauté
    }

    [Fact]
    public void Suivant_IgnoreUnCompteExclu()
    {
        var slots = new[] { Connected("A", 1), Connected("B", 2, excluded: true), Connected("C", 3) };
        Assert.Equal(3, _controller.Decide(Snapshot(slots), Next, foreground: 1).TargetHandle);
    }

    [Fact]
    public void ForegroundNonDofus_LaisssePasser() // CA-02
    {
        var slots = new[] { Connected("A", 1), Connected("B", 2) };
        var decision = _controller.Decide(Snapshot(slots), Next, foreground: 999); // navigateur

        Assert.False(decision.Consume);
        Assert.Equal(SwitchDecision.Pass, decision);
    }

    [Fact]
    public void ForegroundDofus_AvecBinding_ConsommeEtChangeDeFocus() // C-01
    {
        var slots = new[] { Connected("A", 1), Connected("B", 2) };
        var decision = _controller.Decide(Snapshot(slots), Next, foreground: 1);

        Assert.True(decision.Consume);
        Assert.Equal(2, decision.TargetHandle);
    }

    [Fact]
    public void ForegroundDofus_ToucheNonAssociee_LaisssePasser()
    {
        var slots = new[] { Connected("A", 1) };
        var libre = new Binding(BindingKind.Key, 0x41); // A, non assignée
        Assert.False(_controller.Decide(Snapshot(slots), libre, foreground: 1).Consume);
    }

    [Fact]
    public void UnSeulComptePresent_Suivant_ConsommeSansCible()
    {
        var slots = new[] { Connected("A", 1), Absent("B") };
        var decision = _controller.Decide(Snapshot(slots), Next, foreground: 1);

        Assert.True(decision.Consume);   // entrée bindée sur DOFUS → consommée (RG-S01)
        Assert.Equal(0, decision.TargetHandle); // aucune autre cible éligible
    }

    [Fact]
    public void ActivationDirecte_CibleLeCompte_MemeExclu() // US-S02
    {
        var f1 = new Binding(BindingKind.Key, 0x70);
        var slots = new[] { Connected("A", 1), Connected("B", 2, excluded: true) };
        var directs = new Dictionary<Binding, nint> { [f1] = 2 };

        var decision = _controller.Decide(Snapshot(slots, directs), f1, foreground: 1);

        Assert.True(decision.Consume);
        Assert.Equal(2, decision.TargetHandle); // B activé bien qu'exclu de la rotation
    }
}
