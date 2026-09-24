using DofusSwitcher.Constants;
using DofusSwitcher.Models;

// [WARN] App référence WinForms : Binding est ambigu. Alias explicite.
using Binding = DofusSwitcher.Models.Binding;

namespace DofusSwitcher.Tests;

/// <summary>
/// Tests du cœur pur de capture (<see cref="InputCapture"/>) : encodage des boutons souris (cohérent
/// avec les défauts XButton2=2/XButton1=1), rejet des combinaisons/modificateurs seuls (D-01) et
/// formatage lisible d'un <see cref="Binding"/>.
/// </summary>
public class InputCaptureTests
{
    [Fact]
    public void ToBinding_XButtons_CoherentsAvecLesDefautsAppConstants()
    {
        Assert.Equal(new Binding(BindingKind.MouseButton, AppConstants.DefaultNextButtonCode),
            InputCapture.ToBinding(CapturedMouseButton.XButton2));
        Assert.Equal(new Binding(BindingKind.MouseButton, AppConstants.DefaultPrevButtonCode),
            InputCapture.ToBinding(CapturedMouseButton.XButton1));
    }

    [Fact]
    public void ToBinding_BoutonsPrincipaux_CodesDistincts()
    {
        var codes = new[]
        {
            InputCapture.ToBinding(CapturedMouseButton.Left).Code,
            InputCapture.ToBinding(CapturedMouseButton.Right).Code,
            InputCapture.ToBinding(CapturedMouseButton.Middle).Code,
            InputCapture.ToBinding(CapturedMouseButton.XButton1).Code,
            InputCapture.ToBinding(CapturedMouseButton.XButton2).Code,
        };

        Assert.Equal(codes.Length, codes.Distinct().Count()); // encodage sans collision
        Assert.All(codes, c => Assert.Equal(BindingKind.MouseButton, InputCapture.ToBinding(CapturedMouseButton.Left).Kind));
    }

    [Theory]
    [InlineData(0x10)] // Shift
    [InlineData(0x11)] // Control
    [InlineData(0x12)] // Menu (Alt)
    [InlineData(0x5B)] // LWin
    [InlineData(0xA2)] // LControl
    public void IsModifierVirtualKey_VraiPourLesModificateurs(int vk)
        => Assert.True(InputCapture.IsModifierVirtualKey(vk));

    [Fact]
    public void IsModifierVirtualKey_FauxPourUneToucheNormale()
        => Assert.False(InputCapture.IsModifierVirtualKey(0x41)); // A

    [Fact]
    public void ShouldRejectKey_ModificateurSeul_EstRejete()
        => Assert.True(InputCapture.ShouldRejectKey(0x11, modifiersHeld: false)); // Control seul

    [Fact]
    public void ShouldRejectKey_Combinaison_EstRejetee()
        => Assert.True(InputCapture.ShouldRejectKey(0x41, modifiersHeld: true)); // Ctrl+A

    [Fact]
    public void ShouldRejectKey_ToucheNueUnique_EstAcceptee()
        => Assert.False(InputCapture.ShouldRejectKey(0x41, modifiersHeld: false)); // A seul

    [Theory]
    [InlineData(BindingKind.MouseButton, 2, "XButton2")]
    [InlineData(BindingKind.MouseButton, 1, "XButton1")]
    [InlineData(BindingKind.Key, 0x70, "F1")]
    [InlineData(BindingKind.Key, 0x41, "A")]
    [InlineData(BindingKind.Key, 0x30, "0")]
    [InlineData(BindingKind.Key, 0x20, "Espace")]
    public void Format_RenduLisible(BindingKind kind, int code, string expected)
        => Assert.Equal(expected, InputCapture.Format(new Binding(kind, code)));
}
