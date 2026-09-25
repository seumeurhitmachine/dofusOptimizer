using DofusSwitcher.Constants;
using DofusSwitcher.Models;
using DofusSwitcher.ViewModels;

// [WARN] App référence WinForms : Binding est ambigu. Alias explicite.
using Binding = DofusSwitcher.Models.Binding;

namespace DofusSwitcher.Tests;

/// <summary>
/// Tests de l'Axe 10 : personnages sans compte de plein droit dans la rotation (liste unifiée, exclusion,
/// activation directe par personnage, transfert du raccourci au lien) et gating du bouton « Lier ».
/// Logique pure et ViewModels au travers du <see cref="MainViewModel"/> (seul writer de la config).
/// </summary>
public class AccountsAxe10Tests
{
    private static readonly Binding F1 = new(BindingKind.Key, 0x70);
    private static readonly Binding F2 = new(BindingKind.Key, 0x71);
    private static readonly Binding XButton2 = new(BindingKind.MouseButton, AppConstants.DefaultNextButtonCode);

    private static MainViewModel BuildVm(AppConfig config, FakeWindowDetector detector, Binding? captured = null) =>
        new(config, detector, new FakeInputCaptureService(captured), new FakeStartupRegistryService(), new FakeFileDialogService());

    // ---- Rotation : les persos sans compte sont éligibles (non-régression) ----

    [Fact]
    public void PersoSansCompte_ConnecteNonExclu_EstEligibleARotation()
    {
        var config = AppConfig.Default with { Accounts = [new AccountConfig("Iop"), new AccountConfig("Cra")] };
        var detector = new FakeWindowDetector();
        var vm = BuildVm(config, detector);
        detector.RaiseAppeared("Iop", 1);
        detector.RaiseAppeared("Cra", 2);

        var snap = vm.BuildRotationSnapshot();

        Assert.Equal(2, snap.Slots.Count);
        Assert.All(snap.Slots, s => Assert.True(s.IsRotatable)); // connectés, non liés, non exclus
    }

    [Fact]
    public void PersoSansCompte_Exclu_EstSauteParLaRotation()
    {
        var detector = new FakeWindowDetector();
        var vm = BuildVm(AppConfig.Default with { Accounts = [new AccountConfig("Iop")] }, detector);
        detector.RaiseAppeared("Iop", 1);

        vm.Accounts.ToggleExcludeItemCommand.Execute(vm.Accounts.Items.Single());

        var slot = Assert.Single(vm.BuildRotationSnapshot().Slots);
        Assert.True(slot.IsExcluded);
        Assert.False(slot.IsRotatable);
    }

    // ---- Activation directe portée par le PERSONNAGE (sans compte) ----

    [Fact]
    public void PersoSansCompte_ConnecteAvecDirectBinding_EstDansLesDirectsDuSnapshot()
    {
        var config = AppConfig.Default with
        {
            NextBinding = null, PrevBinding = null,
            Accounts = [new AccountConfig("Iop", DirectBinding: F1)], // sans compte, entrée directe
        };
        var detector = new FakeWindowDetector();
        var vm = BuildVm(config, detector);
        detector.RaiseAppeared("Iop", 42);

        var snap = vm.BuildRotationSnapshot();

        Assert.Equal(42, snap.Directs[F1]); // HWND du personnage connecté
    }

    [Fact]
    public void RaccourciDirectSansCompte_AssigneViaCapture_EstMaterialiseSurLePersonnage()
    {
        var config = AppConfig.Default with { NextBinding = null, PrevBinding = null };
        var detector = new FakeWindowDetector();
        var vm = BuildVm(config, detector, captured: F1);
        detector.RaiseAppeared("Iop", 1); // détecté, non lié, pas encore persisté

        var slot = Assert.Single(vm.Shortcuts.DirectSlots);
        Assert.Equal("Iop", slot.Label);

        slot.CaptureCommand.Execute(null);

        Assert.Equal(F1, vm.Config.Accounts.Single(c => c.CharacterName == "Iop").DirectBinding);
        Assert.Equal("F1", slot.Display);
    }

    [Fact]
    public void RaccourciDirectSansCompte_EnConflitAvecSuivant_EstRefuse()
    {
        var config = AppConfig.Default with { PrevBinding = null }; // NextBinding = XButton2 par défaut
        var detector = new FakeWindowDetector();
        var vm = BuildVm(config, detector, captured: XButton2); // = suivant
        detector.RaiseAppeared("Iop", 1);

        var slot = Assert.Single(vm.Shortcuts.DirectSlots);
        slot.CaptureCommand.Execute(null);

        Assert.NotNull(slot.ConflictMessage);
        Assert.Contains("Compte suivant", slot.ConflictMessage);
        Assert.DoesNotContain(vm.Config.Accounts, c => c.DirectBinding is not null); // aucune persistance
    }

    // ---- Transfert du raccourci direct au lien (perso → compte) ----

    [Fact]
    public void LierPersonnageAvecRaccourciDirect_TransfereLeBindingAuCompteSansBinding()
    {
        var config = AppConfig.Default with
        {
            NextBinding = null, PrevBinding = null,
            GameAccounts = [new GameAccount("Compte1")],
            Accounts = [new AccountConfig("Iop", DirectBinding: F1)],
        };
        var detector = new FakeWindowDetector();
        var vm = BuildVm(config, detector);
        detector.RaiseAppeared("Iop", 1);

        vm.Accounts.LinkCharacter("Iop", "Compte1");

        Assert.Equal("Compte1", vm.Config.Accounts.Single(c => c.CharacterName == "Iop").AccountName);
        Assert.Null(vm.Config.Accounts.Single(c => c.CharacterName == "Iop").DirectBinding); // effacé côté perso
        Assert.Equal(F1, vm.Config.GameAccounts.Single().DirectBinding);                     // transféré au compte
    }

    [Fact]
    public void LierPersonnage_QuandLeCompteADejaUnBinding_EffaceLeBindingDuPersoSansEcraser()
    {
        var config = AppConfig.Default with
        {
            NextBinding = null, PrevBinding = null,
            GameAccounts = [new GameAccount("Compte1", DirectBinding: F2)],
            Accounts = [new AccountConfig("Iop", DirectBinding: F1)],
        };
        var detector = new FakeWindowDetector();
        var vm = BuildVm(config, detector);
        detector.RaiseAppeared("Iop", 1);

        vm.Accounts.LinkCharacter("Iop", "Compte1");

        Assert.Null(vm.Config.Accounts.Single(c => c.CharacterName == "Iop").DirectBinding); // effacé
        Assert.Equal(F2, vm.Config.GameAccounts.Single().DirectBinding);                     // inchangé
    }

    // ---- Bouton « Lier » masqué tant qu'aucun compte n'est sélectionné ----

    [Fact]
    public void LigneNonLiee_CanLink_SuitLaSelectionDeCompte()
    {
        var config = AppConfig.Default with { GameAccounts = [new GameAccount("Compte1")] };
        var detector = new FakeWindowDetector();
        var vm = BuildVm(config, detector);
        detector.RaiseAppeared("Iop", 1);

        var row = Assert.Single(vm.Accounts.ConnectedRows, r => !r.IsLinked);
        Assert.False(row.CanLink); // aucun compte sélectionné → « Lier » masqué

        row.SelectedAccount = "Compte1";
        Assert.True(row.CanLink);  // compte choisi → « Lier » visible
    }

    // ---- Un perso sans compte n'apparaît jamais dans les comptes déconnectés ----

    [Fact]
    public void PersoSansCompteDeconnecte_NApparaitPasDansLesComptesDeconnectes()
    {
        var config = AppConfig.Default with
        {
            GameAccounts = [new GameAccount("Compte1")],
            Accounts =
            [
                new AccountConfig("Iop", AccountName: "Compte1"), // lié, absent → compte déconnecté
                new AccountConfig("Solo"),                        // sans compte, absent
            ],
        };
        var vm = BuildVm(config, new FakeWindowDetector()); // rien de connecté

        Assert.Empty(vm.Accounts.ConnectedRows);
        Assert.Equal(new[] { "Compte1" }, vm.Accounts.DisconnectedAccounts); // seul le compte lié
        Assert.DoesNotContain("Solo", vm.Accounts.DisconnectedAccounts);     // le perso sans compte n'y figure pas
    }
}
