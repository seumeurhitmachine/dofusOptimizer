using DofusSwitcher.Models;
using DofusSwitcher.ViewModels;

namespace DofusSwitcher.Tests;

/// <summary>
/// Tests du câblage du VM Comptes sur le détecteur : la liste reflète en temps réel l'apparition et
/// la disparition des fenêtres, et conserve les comptes persistés absents (US-D02, RG-D03).
/// </summary>
public class AccountsViewModelTests
{
    [Fact]
    public void ComptePersiste_EstAffiche_AbsentTantQueRienNestDetecte()
    {
        var detector = new FakeWindowDetector();
        var vm = new AccountsViewModel([new AccountConfig("Iop-Kamar")], detector);

        var item = Assert.Single(vm.Items);
        Assert.Equal("Iop-Kamar", item.CharacterName);
        Assert.False(item.IsConnected);
    }

    [Fact]
    public void Apparition_ConnecteLeCompte_PuisDisparition_LeRepasseAbsent()
    {
        var detector = new FakeWindowDetector();
        var vm = new AccountsViewModel([new AccountConfig("Iop-Kamar")], detector);

        detector.RaiseAppeared("Iop-Kamar", 42);
        Assert.True(vm.Items[0].IsConnected);
        Assert.Equal(42, vm.Items[0].Handle);

        detector.RaiseDisappeared("Iop-Kamar", 42);
        Assert.False(vm.Items[0].IsConnected); // conservé, repassé absent (RG-D03)
        Assert.Single(vm.Items);
    }

    [Fact]
    public void FenetreDetecteeNonPersistee_EstAjouteeEnFin_PuisRetireeALaFermeture()
    {
        var detector = new FakeWindowDetector();
        var vm = new AccountsViewModel([], detector);
        Assert.True(vm.IsEmpty);

        detector.RaiseAppeared("Cra-Lena", 7);
        var item = Assert.Single(vm.Items);
        Assert.Equal("Cra-Lena", item.CharacterName);
        Assert.True(item.IsConnected);

        // Non persistée : sa fermeture la retire complètement de la liste.
        detector.RaiseDisappeared("Cra-Lena", 7);
        Assert.Empty(vm.Items);
        Assert.True(vm.IsEmpty);
    }
}
