using DofusSwitcher.Models;
using DofusSwitcher.ViewModels;

namespace DofusSwitcher.Tests;

/// <summary>
/// Tests du réordonnancement (US-D03) et de l'exclusion (US-D04) dans l'onglet Comptes : effet
/// immédiat, bornes des commandes, persistance émise (ordre + flag), matérialisation d'un compte
/// détecté à la première action, et préservation de l'état runtime après réordonnancement.
/// </summary>
public class AccountsOrderingTests
{
    private static AccountsViewModel BuildWith(params string[] names)
    {
        var persisted = names.Select(n => new AccountConfig(n)).ToArray();
        return new AccountsViewModel(persisted, new FakeWindowDetector());
    }

    [Fact]
    public void MoveUp_RemonteLeCompteSelectionne_EtEmetLeNouvelOrdre()
    {
        var vm = BuildWith("A", "B", "C");
        IReadOnlyList<AccountConfig>? persisted = null;
        vm.AccountsChanged += p => persisted = p;

        vm.SelectedItem = vm.Items[2]; // C
        vm.MoveUpCommand.Execute(null);

        Assert.Equal(["A", "C", "B"], vm.Items.Select(i => i.CharacterName));
        Assert.Equal(["A", "C", "B"], persisted!.Select(a => a.CharacterName));
    }

    [Fact]
    public void MoveDown_DescendLeCompteSelectionne()
    {
        var vm = BuildWith("A", "B", "C");
        vm.SelectedItem = vm.Items[0]; // A
        vm.MoveDownCommand.Execute(null);

        Assert.Equal(["B", "A", "C"], vm.Items.Select(i => i.CharacterName));
    }

    [Fact]
    public void Commandes_RespectentLesBornes()
    {
        var vm = BuildWith("A", "B");

        vm.SelectedItem = null;
        Assert.False(vm.MoveUpCommand.CanExecute(null));
        Assert.False(vm.MoveDownCommand.CanExecute(null));
        Assert.False(vm.ToggleExcludeCommand.CanExecute(null));

        vm.SelectedItem = vm.Items[0]; // en tête
        Assert.False(vm.MoveUpCommand.CanExecute(null));
        Assert.True(vm.MoveDownCommand.CanExecute(null));

        vm.SelectedItem = vm.Items[1]; // en fin
        Assert.True(vm.MoveUpCommand.CanExecute(null));
        Assert.False(vm.MoveDownCommand.CanExecute(null));
    }

    [Fact]
    public void ToggleExclude_BasculeLeFlag_EtLePersiste()
    {
        var vm = BuildWith("A");
        IReadOnlyList<AccountConfig>? persisted = null;
        vm.AccountsChanged += p => persisted = p;

        vm.SelectedItem = vm.Items[0];
        vm.ToggleExcludeCommand.Execute(null);

        Assert.True(vm.Items[0].IsExcluded);
        Assert.True(persisted!.Single().Excluded);

        vm.ToggleExcludeCommand.Execute(null); // réintégration
        Assert.False(vm.Items[0].IsExcluded);
        Assert.False(persisted!.Single().Excluded);
    }

    [Fact]
    public void CompteDetecteNonPersiste_EstMaterialiseALaPremiereAction()
    {
        var detector = new FakeWindowDetector();
        var vm = new AccountsViewModel([], detector);
        IReadOnlyList<AccountConfig>? persisted = null;
        vm.AccountsChanged += p => persisted = p;

        detector.RaiseAppeared("Iop-Kamar", 5); // détecté, pas encore persisté
        Assert.Null(persisted); // la détection seule ne persiste pas

        vm.SelectedItem = vm.Items[0];
        vm.ToggleExcludeCommand.Execute(null); // 1re action → matérialisation

        Assert.Equal("Iop-Kamar", persisted!.Single().CharacterName);
        Assert.True(persisted!.Single().Excluded);
    }

    [Fact]
    public void Reordonnancement_PreserveLetatRuntime()
    {
        var detector = new FakeWindowDetector();
        var vm = new AccountsViewModel([new AccountConfig("A"), new AccountConfig("B")], detector);
        detector.RaiseAppeared("B", 99); // B connecté avec un handle

        vm.SelectedItem = vm.Items.First(i => i.CharacterName == "B");
        vm.MoveUpCommand.Execute(null);

        var b = vm.Items[0];
        Assert.Equal("B", b.CharacterName);
        Assert.True(b.IsConnected);   // état runtime conservé après le déplacement
        Assert.Equal(99, b.Handle);
    }

    [Fact]
    public void MoveItem_DnD_DeplaceALindexCible()
    {
        var vm = BuildWith("A", "B", "C");
        vm.MoveItem(0, 2); // A déplacé en dernière position

        Assert.Equal(["B", "C", "A"], vm.Items.Select(i => i.CharacterName));
    }
}
