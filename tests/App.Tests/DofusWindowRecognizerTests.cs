using DofusSwitcher.Services;

namespace DofusSwitcher.Tests;

/// <summary>
/// Tests des fonctions pures de reconnaissance : extraction du nom de personnage (RG-D01) et
/// critère « fenêtre DOFUS » (C-02, sans inspection process — spec §3.x).
/// </summary>
public class DofusWindowRecognizerTests
{
    [Theory]
    [InlineData("Iop-Kamar - Dofus 2.68", "Iop-Kamar")]
    [InlineData("Cra-Lena - Dofus", "Cra-Lena")]
    [InlineData("  Eni-Bob   - Dofus 3.0.0  ", "Eni-Bob")] // espaces de bord nettoyés
    [InlineData("Sacri Multi-Mots - Dofus 2.68", "Sacri Multi-Mots")]
    public void ExtractCharacterName_IsoleLeNom(string title, string expected)
    {
        Assert.Equal(expected, DofusWindowRecognizer.ExtractCharacterName(title));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("Dofus")]                 // launcher : pas de séparateur
    [InlineData("Bloc-notes")]            // fenêtre étrangère
    [InlineData(" - Dofus 2.68")]         // marqueur en tête : nom vide
    public void ExtractCharacterName_RetourneNull_HorsFormat(string? title)
    {
        Assert.Null(DofusWindowRecognizer.ExtractCharacterName(title));
    }

    [Fact]
    public void IsDofusWindow_VraiPourUnTitreDofusAvecNom()
    {
        Assert.True(DofusWindowRecognizer.IsDofusWindow("Iop-Kamar - Dofus 2.68", "ApolloRuntimeContentWindow"));
    }

    [Theory]
    [InlineData("Dofus", "UnityWndClass")]        // launcher
    [InlineData("Chrome", "Chrome_WidgetWin_1")]  // fenêtre étrangère
    [InlineData(" - Dofus 2.68", "any")]          // nom vide
    public void IsDofusWindow_FauxHorsFormat(string title, string className)
    {
        Assert.False(DofusWindowRecognizer.IsDofusWindow(title, className));
    }
}
