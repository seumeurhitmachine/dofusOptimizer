using System.IO;
using DofusSwitcher.Models;
using DofusSwitcher.Persistence;
using DofusSwitcher.Services;
using DofusSwitcher.ViewModels;

namespace DofusSwitcher.Tests;

/// <summary>
/// Tests de l'Axe 8 (comptes ↔ personnages) : validation de nom, partition en 3 zones, CRUD comptes
/// (avec cascade), liaison, et migration montante v1→v2. Logique pure et ViewModels, sans WPF.
/// </summary>
public class AccountsV2Tests
{
    private static MainViewModel BuildVm(AppConfig config, FakeWindowDetector detector) =>
        new(config, detector, new FakeInputCaptureService(null), new FakeStartupRegistryService(), new FakeFileDialogService());

    // ---- Validation du nom de compte (RG-C01) ----

    [Theory]
    [InlineData("Compte1", true)]
    [InlineData("Mon Compte", true)]
    [InlineData("multi-compte 2", true)]
    [InlineData("  Espaces autour  ", true)] // valide après Trim
    [InlineData("", false)]
    [InlineData("   ", false)]
    [InlineData("accentué", false)]          // 'é' hors [A-Za-z0-9 -]
    [InlineData("under_score", false)]
    [InlineData("slash/x", false)]
    public void GameAccount_IsValidName(string name, bool expected)
        => Assert.Equal(expected, GameAccount.IsValidName(name));

    [Fact]
    public void GameAccount_RejetteAuDela40Caracteres()
    {
        Assert.True(GameAccount.IsValidName(new string('a', 40)));
        Assert.False(GameAccount.IsValidName(new string('a', 41)));
    }

    // ---- Partition en 3 zones (comptes-v2 §2.6) ----

    [Fact]
    public void AccountZones_RepartitLes3Zones_EtCalculeLesDisponibles()
    {
        var accounts = new List<GameAccount> { new("A"), new("B"), new("C"), new("Vide") };
        var characters = new List<AccountRuntimeState>
        {
            new("Iop", IsConnected: true, IsExcluded: false, Handle: 1, AccountName: "A"),  // zone 1 (A)
            new("Enu", IsConnected: true, IsExcluded: false, Handle: 2, AccountName: null), // zone 2
            new("Cra", IsConnected: false, IsExcluded: false, Handle: 0, AccountName: "B"), // zone 3 (B)
            new("Sram", IsConnected: false, IsExcluded: false, Handle: 0, AccountName: "B"),// zone 3 (B)
        };

        var zones = AccountZones.Build(accounts, characters);

        Assert.Equal(new[] { "A" }, zones.Connected.Select(z => z.AccountName));
        Assert.Equal("Iop", zones.Connected[0].ConnectedCharacter);
        Assert.Equal(new[] { "Enu" }, zones.UnlinkedConnected);
        Assert.Equal(new[] { "B" }, zones.Disconnected.Select(z => z.AccountName));
        Assert.Equal(new[] { "Cra", "Sram" }, zones.Disconnected[0].LinkedCharacters);
        // A occupé (perso connecté) → indisponible ; B, C, Vide disponibles.
        Assert.Equal(new[] { "B", "C", "Vide" }, zones.AvailableAccounts);
    }

    [Fact]
    public void AccountZones_LienOrphelin_TraiteLePersonnageCommeSansCompte()
    {
        var accounts = new List<GameAccount>(); // aucun compte
        var characters = new List<AccountRuntimeState>
        {
            new("Iop", IsConnected: true, IsExcluded: false, Handle: 1, AccountName: "Supprimé"),
        };

        var zones = AccountZones.Build(accounts, characters);

        Assert.Equal(new[] { "Iop" }, zones.UnlinkedConnected);
        Assert.Empty(zones.Connected);
    }

    // ---- CRUD comptes via Réglages (seul writer = MainViewModel) ----

    [Fact]
    public void CreerCompte_ValideEtPersiste()
    {
        var vm = BuildVm(AppConfig.Default, new FakeWindowDetector());

        vm.Settings.NewAccountName = "Compte1";
        vm.Settings.CreateAccountCommand.Execute(null);

        Assert.Null(vm.Settings.AccountError);
        Assert.Equal("Compte1", Assert.Single(vm.Config.GameAccounts).Name);
        Assert.Contains(vm.Settings.GameAccounts, r => r.Name == "Compte1");
        Assert.Contains("Compte1", vm.Accounts.AvailableAccounts);
    }

    [Theory]
    [InlineData("bad!name")]
    [InlineData("")]
    public void CreerCompte_NomInvalide_Refuse(string name)
    {
        var vm = BuildVm(AppConfig.Default, new FakeWindowDetector());

        vm.Settings.NewAccountName = name;
        vm.Settings.CreateAccountCommand.Execute(null);

        Assert.NotNull(vm.Settings.AccountError);
        Assert.Empty(vm.Config.GameAccounts);
    }

    [Fact]
    public void CreerCompte_DoublonInsensibleCasse_Refuse()
    {
        var vm = BuildVm(AppConfig.Default, new FakeWindowDetector());
        vm.Settings.NewAccountName = "Compte1";
        vm.Settings.CreateAccountCommand.Execute(null);

        vm.Settings.NewAccountName = "compte1";
        vm.Settings.CreateAccountCommand.Execute(null);

        Assert.NotNull(vm.Settings.AccountError);
        Assert.Single(vm.Config.GameAccounts);
    }

    [Fact]
    public void SupprimerCompte_SupprimeLesPersonnagesLies_QuiReapparaissentSansCompteSiConnectes()
    {
        var detector = new FakeWindowDetector();
        var vm = BuildVm(AppConfig.Default, detector);
        vm.Settings.NewAccountName = "Compte1";
        vm.Settings.CreateAccountCommand.Execute(null);
        detector.RaiseAppeared("Iop", 1);
        vm.Accounts.LinkCharacter("Iop", "Compte1");
        Assert.Single(vm.Accounts.ConnectedAccounts);

        vm.Settings.GameAccounts.Single(r => r.Name == "Compte1").DeleteCommand.Execute(null);

        Assert.Empty(vm.Config.GameAccounts);
        Assert.DoesNotContain(vm.Config.Accounts, a => a.CharacterName == "Iop"); // cascade : config retirée
        Assert.Empty(vm.Accounts.ConnectedAccounts);
        Assert.Single(vm.Accounts.UnlinkedConnected); // encore connecté → réapparaît non lié (zone 2)
    }

    [Fact]
    public void SupprimerPersonnage_DepuisLeCompteDeplie_LeRetireEtLeRemetEnZone2SiConnecte()
    {
        var detector = new FakeWindowDetector();
        var vm = BuildVm(AppConfig.Default, detector);
        vm.Settings.NewAccountName = "Compte1";
        vm.Settings.CreateAccountCommand.Execute(null);
        detector.RaiseAppeared("Iop", 1);
        vm.Accounts.LinkCharacter("Iop", "Compte1");

        var row = vm.Settings.GameAccounts.Single(r => r.Name == "Compte1");
        var character = Assert.Single(row.LinkedCharacters);
        Assert.Equal("Iop", character.Name);

        character.DeleteCommand.Execute(null);

        Assert.DoesNotContain(vm.Config.Accounts, a => a.CharacterName == "Iop");
        Assert.Empty(vm.Accounts.ConnectedAccounts);
        Assert.Single(vm.Accounts.UnlinkedConnected); // encore connecté → zone 2
        // Le compte reste, désormais sans personnage lié.
        Assert.Empty(vm.Settings.GameAccounts.Single(r => r.Name == "Compte1").LinkedCharacters);
    }

    // ---- Liaison (US-C05) ----

    [Fact]
    public void LierPersonnage_LeDeplaceEnZone1_EtOccupeLeCompte()
    {
        var detector = new FakeWindowDetector();
        var vm = BuildVm(AppConfig.Default, detector);
        vm.Settings.NewAccountName = "Compte1";
        vm.Settings.CreateAccountCommand.Execute(null);
        detector.RaiseAppeared("Iop", 1);
        Assert.Single(vm.Accounts.UnlinkedConnected);
        Assert.Contains("Compte1", vm.Accounts.AvailableAccounts);

        vm.Accounts.LinkCharacter("Iop", "Compte1");

        Assert.Empty(vm.Accounts.UnlinkedConnected);
        Assert.Equal("Compte1", Assert.Single(vm.Accounts.ConnectedAccounts).AccountName);
        Assert.DoesNotContain("Compte1", vm.Accounts.AvailableAccounts); // compte occupé (perso connecté)
        Assert.Equal("Compte1", vm.Config.Accounts.Single(a => a.CharacterName == "Iop").AccountName);
    }

    // ---- Migration v1 → v2 ----

    [Fact]
    public void Migration_V1SansComptes_ChargeEnV2_PersonnagesNonLies()
    {
        var path = Path.Combine(Path.GetTempPath(), $"dofus-v1-{Guid.NewGuid():N}.json");
        // Config v1 réelle : schemaVersion 1, pas de champ gameAccounts ni accountName.
        File.WriteAllText(path,
            """
            {
              "schemaVersion": 1,
              "interceptionSuspended": false,
              "startWithWindows": false,
              "nextBinding": null,
              "prevBinding": null,
              "accounts": [ { "characterName": "Iop", "excluded": false, "directBinding": null } ]
            }
            """);
        try
        {
            var config = new JsonConfigStore(path).Load();

            Assert.Equal(AppConfig.CurrentSchemaVersion, config.SchemaVersion); // 2
            Assert.NotNull(config.GameAccounts);
            Assert.Empty(config.GameAccounts);
            var iop = Assert.Single(config.Accounts);
            Assert.Equal("Iop", iop.CharacterName);
            Assert.Null(iop.AccountName); // non lié
        }
        finally
        {
            File.Delete(path);
        }
    }
}
