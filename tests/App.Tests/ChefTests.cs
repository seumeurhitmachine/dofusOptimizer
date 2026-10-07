using System.IO;
using DofusSwitcher.Models;
using DofusSwitcher.Persistence;
using DofusSwitcher.ViewModels;

namespace DofusSwitcher.Tests;

/// <summary>
/// Tests de l'Axe 13 (compte chef + copie <c>/invite</c>) : désignation unique du chef (toggle), effacement
/// à la suppression du compte, persistance du champ additif <c>ChefAccountName</c> (schéma inchangé), couronne
/// sur la bonne ligne, et construction de la formule <c>/invite</c> (autres personnages connectés, chef exclu).
/// Logique pure et ViewModels, sans WPF.
/// </summary>
public class ChefTests
{
    private static MainViewModel BuildVm(FakeWindowDetector detector, FakeClipboardService? clipboard = null) =>
        new(AppConfig.Default, detector, new FakeInputCaptureService(null), new FakeStartupRegistryService(),
            new FakeFileDialogService(), clipboard: clipboard);

    private static void CreateAccount(MainViewModel vm, string name)
    {
        vm.Settings.NewAccountName = name;
        vm.Settings.CreateAccountCommand.Execute(null);
    }

    private static void ToggleChef(MainViewModel vm, string accountName) =>
        vm.Settings.GameAccounts.Single(r => r.Name == accountName).ToggleChefCommand.Execute(null);

    // ---- Désignation / unicité / toggle ----

    [Fact]
    public void DesignerChef_UnSeulAlaFois()
    {
        var vm = BuildVm(new FakeWindowDetector());
        CreateAccount(vm, "A");
        CreateAccount(vm, "B");

        ToggleChef(vm, "A");
        Assert.Equal("A", vm.Config.ChefAccountName);
        Assert.True(vm.Settings.GameAccounts.Single(r => r.Name == "A").IsChef);

        // Désigner B remplace A (un seul chef).
        ToggleChef(vm, "B");
        Assert.Equal("B", vm.Config.ChefAccountName);
        Assert.False(vm.Settings.GameAccounts.Single(r => r.Name == "A").IsChef);
        Assert.True(vm.Settings.GameAccounts.Single(r => r.Name == "B").IsChef);
    }

    [Fact]
    public void RecliquerLeChef_LeRetire()
    {
        var vm = BuildVm(new FakeWindowDetector());
        CreateAccount(vm, "A");

        ToggleChef(vm, "A");
        Assert.Equal("A", vm.Config.ChefAccountName);

        ToggleChef(vm, "A");
        Assert.Null(vm.Config.ChefAccountName);
        Assert.False(vm.Settings.GameAccounts.Single(r => r.Name == "A").IsChef);
    }

    [Fact]
    public void SupprimerLeCompteChef_EffaceLaReference()
    {
        var vm = BuildVm(new FakeWindowDetector());
        CreateAccount(vm, "A");
        CreateAccount(vm, "B");
        ToggleChef(vm, "A");

        vm.Settings.GameAccounts.Single(r => r.Name == "A").DeleteCommand.Execute(null);

        Assert.Null(vm.Config.ChefAccountName);
    }

    [Fact]
    public void SupprimerUnAutreCompte_PreserveLeChef()
    {
        var vm = BuildVm(new FakeWindowDetector());
        CreateAccount(vm, "A");
        CreateAccount(vm, "B");
        ToggleChef(vm, "A");

        vm.Settings.GameAccounts.Single(r => r.Name == "B").DeleteCommand.Execute(null);

        Assert.Equal("A", vm.Config.ChefAccountName);
    }

    // ---- Persistance (champ additif, schéma inchangé) ----

    [Fact]
    public void ChefAccountName_RoundTrip_SansBumpSchema()
    {
        var path = Path.Combine(Path.GetTempPath(), $"dofus-chef-{Guid.NewGuid():N}.json");
        try
        {
            var config = AppConfig.Default with
            {
                GameAccounts = [new GameAccount("A")],
                ChefAccountName = "A",
            };
            new JsonConfigStore(path).Save(config);

            var reloaded = new JsonConfigStore(path).Load();

            Assert.Equal("A", reloaded.ChefAccountName);
            Assert.Equal(AppConfig.CurrentSchemaVersion, reloaded.SchemaVersion); // reste 3, pas de migration
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void ConfigSansChef_ChargeAvecChefNull()
    {
        var path = Path.Combine(Path.GetTempPath(), $"dofus-nochef-{Guid.NewGuid():N}.json");
        // Config v3 sans le champ chefAccountName : doit charger avec null (additif).
        File.WriteAllText(path,
            """
            {
              "schemaVersion": 3,
              "interceptionSuspended": false,
              "startWithWindows": false,
              "nextBinding": null,
              "prevBinding": null,
              "accounts": [],
              "gameAccounts": []
            }
            """);
        try
        {
            var config = new JsonConfigStore(path).Load();
            Assert.Null(config.ChefAccountName);
        }
        finally
        {
            File.Delete(path);
        }
    }

    // ---- Couronne (onglet Comptes) ----

    [Fact]
    public void Couronne_SurLePersonnageDuCompteChefConnecte()
    {
        var detector = new FakeWindowDetector();
        var vm = BuildVm(detector);
        CreateAccount(vm, "Chef");
        detector.RaiseAppeared("ChefChar", 1);
        vm.Accounts.LinkCharacter("ChefChar", "Chef");

        ToggleChef(vm, "Chef");

        var chefRow = Assert.Single(vm.Accounts.ConnectedRows, r => r.IsChef);
        Assert.Equal("ChefChar", chefRow.Character.CharacterName);
    }

    [Fact]
    public void ChefSeulConnecte_PasDeBoutonInvite_MaisCouronne()
    {
        var detector = new FakeWindowDetector();
        var vm = BuildVm(detector);
        CreateAccount(vm, "Chef");
        detector.RaiseAppeared("ChefChar", 1);
        vm.Accounts.LinkCharacter("ChefChar", "Chef");
        ToggleChef(vm, "Chef");

        var chefRow = Assert.Single(vm.Accounts.ConnectedRows, r => r.IsChef);
        Assert.True(chefRow.IsChef);        // couronne affichée
        Assert.False(chefRow.ShowInvite);   // pas d'autre perso → bouton /invite masqué
    }

    [Fact]
    public void ChefAvecClientAnonyme_PasDeBoutonInvite()
    {
        var detector = new FakeWindowDetector();
        var vm = BuildVm(detector);
        CreateAccount(vm, "Chef");
        detector.RaiseAppeared("ChefChar", 1);
        vm.Accounts.LinkCharacter("ChefChar", "Chef");
        detector.RaiseAnonymousAppeared(2); // client sans personnage → non invitable
        ToggleChef(vm, "Chef");

        var chefRow = Assert.Single(vm.Accounts.ConnectedRows, r => r.IsChef);
        Assert.False(chefRow.ShowInvite);
    }

    // ---- Formule /invite ----

    [Fact]
    public void CopieInvite_ListeLesAutresPersosConnectes_ChefExclu()
    {
        var detector = new FakeWindowDetector();
        var clipboard = new FakeClipboardService();
        var vm = BuildVm(detector, clipboard);
        CreateAccount(vm, "Chef");
        CreateAccount(vm, "Second");
        detector.RaiseAppeared("ChefChar", 1);
        vm.Accounts.LinkCharacter("ChefChar", "Chef");
        detector.RaiseAppeared("LieChar", 2);
        vm.Accounts.LinkCharacter("LieChar", "Second");
        detector.RaiseAppeared("SoloChar", 3); // connecté sans compte
        ToggleChef(vm, "Chef");

        var chefRow = Assert.Single(vm.Accounts.ConnectedRows, r => r.IsChef);
        Assert.True(chefRow.ShowInvite);
        chefRow.CopyInviteCommand.Execute(null);

        // Ordre = ordre de rotation (apparition) ; chef exclu.
        Assert.Equal("/invite LieChar; /invite SoloChar", clipboard.LastText);
    }
}
