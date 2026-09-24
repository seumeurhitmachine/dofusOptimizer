using DofusSwitcher.Services;

namespace DofusSwitcher.Tests;

/// <summary>
/// Tests des fonctions pures de reconnaissance du client DOFUS Unity : extraction du nom de personnage
/// (RG-D01) et critère « fenêtre DOFUS » (classe UnityWndClass + format de titre en jeu, C-02, sans
/// inspection process — spec §3.x). Format réel observé : « Nom - Classe - Version - Type ».
/// </summary>
public class DofusWindowRecognizerTests
{
    [Theory]
    [InlineData("Seumeurblood - Sacrieur - 3.6.12.16 - Release", "Seumeurblood")]
    [InlineData("Seumeurstorm - Huppermage - 3.6.12.16 - Release", "Seumeurstorm")]
    [InlineData("Seumeurknight - Forgelance - 3.6.12.16 - Release", "Seumeurknight")]
    [InlineData("Iop-Kamar - Iop - 2.70.0 - Release", "Iop-Kamar")] // tiret sans espaces conservé dans le nom
    [InlineData("  Lena - Crâ - 3.6.12.16 - Beta  ", "Lena")]       // espaces de bord nettoyés, type ≠ Release
    public void ExtractCharacterName_IsoleLeNom(string title, string expected)
    {
        Assert.Equal(expected, DofusWindowRecognizer.ExtractCharacterName(title));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("Dofus")]                              // launcher / écran de démarrage
    [InlineData("Ankama Launcher")]                    // launcher
    [InlineData("Seumeurblood - Sacrieur")]            // trop peu de segments (pas en jeu)
    [InlineData("Seumeurblood - Sacrieur - Release")]  // 3 segments, pas de version
    [InlineData("A - B - C - D")]                      // avant-dernier segment n'est pas une version
    [InlineData(" - Sacrieur - 3.6.12.16 - Release")]  // nom vide
    public void ExtractCharacterName_RetourneNull_HorsFormat(string? title)
    {
        Assert.Null(DofusWindowRecognizer.ExtractCharacterName(title));
    }

    [Fact]
    public void IsDofusWindow_VraiPourClientUnityEnJeu()
    {
        Assert.True(DofusWindowRecognizer.IsDofusWindow("Seumeurblood - Sacrieur - 3.6.12.16 - Release", "UnityWndClass"));
    }

    [Theory]
    [InlineData("Seumeurblood - Sacrieur - 3.6.12.16 - Release", "Chrome_WidgetWin_1")] // bon titre, mauvaise classe (launcher)
    [InlineData("Ankama Launcher", "Chrome_WidgetWin_1")]                               // launcher
    [InlineData("Some Unity Game", "UnityWndClass")]                                    // autre jeu Unity, titre hors format
    [InlineData(" - Sacrieur - 3.6.12.16 - Release", "UnityWndClass")]                  // nom vide
    public void IsDofusWindow_FauxHorsCritere(string title, string className)
    {
        Assert.False(DofusWindowRecognizer.IsDofusWindow(title, className));
    }
}
