using DofusSwitcher.Models;
using DofusSwitcher.Services;

namespace DofusSwitcher.Tests;

/// <summary>
/// Tests de la fusion pure config/fenêtres : apparition, disparition, conservation hors ligne,
/// ordre persistant, comptes détectés non persistés (RG-D02/RG-D03, spec §3.x).
/// </summary>
public class AccountMergeTests
{
    private static AccountConfig Persisted(string name, bool excluded = false) => new(name, excluded);
    private static DetectedWindow Window(string name, nint handle) => new(name, handle);

    [Fact]
    public void CompteDetecteEtPersiste_EstConnecteAvecSonHandle()
    {
        var result = AccountMerge.Merge(
            [Persisted("Iop-Kamar")],
            [Window("Iop-Kamar", 42)]);

        var account = Assert.Single(result);
        Assert.Equal("Iop-Kamar", account.CharacterName);
        Assert.True(account.IsConnected);
        Assert.Equal(42, account.Handle);
    }

    [Fact]
    public void ComptePersisteSansFenetre_EstConserveAbsent()
    {
        var result = AccountMerge.Merge(
            [Persisted("Iop-Kamar")],
            []);

        var account = Assert.Single(result); // conservé même absent (RG-D03)
        Assert.False(account.IsConnected);
        Assert.Equal(0, account.Handle);
    }

    [Fact]
    public void FenetreDetecteeSansCompte_EstAjouteeConnecteeEnFin()
    {
        var result = AccountMerge.Merge(
            [Persisted("Iop-Kamar")],
            [Window("Iop-Kamar", 1), Window("Cra-Lena", 2)]);

        Assert.Equal(2, result.Count);
        Assert.Equal("Iop-Kamar", result[0].CharacterName);
        Assert.Equal("Cra-Lena", result[1].CharacterName); // détectée mais non persistée → en fin
        Assert.True(result[1].IsConnected);
        Assert.False(result[1].IsExcluded);
    }

    [Fact]
    public void OrdrePersistant_EstLaSourceDeVerite()
    {
        var result = AccountMerge.Merge(
            [Persisted("C"), Persisted("A"), Persisted("B")],
            [Window("A", 1), Window("B", 2), Window("C", 3)]); // ordre de détection différent

        Assert.Equal(["C", "A", "B"], result.Select(a => a.CharacterName));
    }

    [Fact]
    public void ExclusionPersistee_EstRecopiee()
    {
        var result = AccountMerge.Merge(
            [Persisted("Eni-Bob", excluded: true)],
            []);

        Assert.True(Assert.Single(result).IsExcluded);
    }

    [Fact]
    public void Apparition_PuisDisparition_BasculeLetatConnecte()
    {
        var persisted = new[] { Persisted("Iop-Kamar") };

        // Apparition : la fenêtre existe → connecté.
        var appeared = AccountMerge.Merge(persisted, [Window("Iop-Kamar", 7)]);
        Assert.True(appeared[0].IsConnected);

        // Disparition : plus aucune fenêtre → absent, mais toujours présent dans la liste.
        var disappeared = AccountMerge.Merge(persisted, []);
        Assert.False(disappeared[0].IsConnected);
        Assert.Equal("Iop-Kamar", disappeared[0].CharacterName);
    }
}
