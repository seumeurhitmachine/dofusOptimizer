using DofusSwitcher.Constants;
using DofusSwitcher.Models;
using DofusSwitcher.ViewModels;

// [WARN] App référence WinForms : Binding est ambigu. Alias explicite.
using Binding = DofusSwitcher.Models.Binding;

namespace DofusSwitcher.Tests;

/// <summary>
/// Tests de l'onglet Raccourcis (US-S04/S05) au travers du <see cref="MainViewModel"/> (seul writer de
/// la config) : assignation via capture, refus de conflit (RG-S05), annulation, effacement, et
/// activation directe par compte (matérialisée). La capture est simulée par un faux service.
/// </summary>
public class ShortcutsViewModelTests
{
    private static readonly Binding KeyA = new(BindingKind.Key, 0x41);
    private static readonly Binding XButton2 = new(BindingKind.MouseButton, AppConstants.DefaultNextButtonCode);
    private static readonly Binding XButton1 = new(BindingKind.MouseButton, AppConstants.DefaultPrevButtonCode);

    private static (MainViewModel vm, List<AppConfig> emitted) Build(AppConfig config, Binding? captured)
    {
        var detector = new FakeWindowDetector();
        var capture = new FakeInputCaptureService(captured);
        var vm = new MainViewModel(config, detector, capture, new FakeStartupRegistryService(), new FakeFileDialogService());
        var emitted = new List<AppConfig>();
        vm.ConfigChanged += emitted.Add;
        return (vm, emitted);
    }

    [Fact]
    public void CaptureSuivant_AssigneEtAffiche_EtEmetConfigChanged()
    {
        // Défaut : next=XButton2, prev=XButton1. Capturer « A » pour suivant (aucun conflit).
        var (vm, emitted) = Build(AppConfig.Default, KeyA);

        vm.Shortcuts.NextSlot.CaptureCommand.Execute(null);

        Assert.Equal(KeyA, vm.Config.NextBinding);
        Assert.Equal("A", vm.Shortcuts.NextSlot.Display);
        Assert.Null(vm.Shortcuts.NextSlot.ConflictMessage);
        Assert.Single(emitted);
        Assert.Equal(KeyA, emitted[0].NextBinding);
    }

    [Fact]
    public void CaptureBoutonAuxiliaire_EstAcceptee()
    {
        // Repartir sans bindings globaux pour éviter tout conflit ; capturer XButton1 (auxiliaire).
        var config = AppConfig.Default with { NextBinding = null, PrevBinding = null };
        var (vm, _) = Build(config, XButton1);

        vm.Shortcuts.NextSlot.CaptureCommand.Execute(null);

        Assert.Equal(XButton1, vm.Config.NextBinding);
        Assert.Equal("XButton1", vm.Shortcuts.NextSlot.Display);
    }

    [Fact]
    public void CaptureEntreeDejaAssignee_SignaleConflit_EtRefuseEnregistrement()
    {
        // Capturer XButton1 (déjà = précédent) pour l'emplacement suivant → conflit RG-S05.
        var (vm, emitted) = Build(AppConfig.Default, XButton1);

        vm.Shortcuts.NextSlot.CaptureCommand.Execute(null);

        Assert.Equal(XButton2, vm.Config.NextBinding); // inchangé
        Assert.NotNull(vm.Shortcuts.NextSlot.ConflictMessage);
        Assert.Contains("Compte précédent", vm.Shortcuts.NextSlot.ConflictMessage);
        Assert.Empty(emitted); // aucune persistance
    }

    [Fact]
    public void CaptureAnnulee_NeChangeRien()
    {
        var (vm, emitted) = Build(AppConfig.Default, captured: null); // Échap

        vm.Shortcuts.NextSlot.CaptureCommand.Execute(null);

        Assert.Equal(XButton2, vm.Config.NextBinding);
        Assert.Null(vm.Shortcuts.NextSlot.ConflictMessage);
        Assert.Empty(emitted);
    }

    [Fact]
    public void Effacer_RetireLEntree_EtEmetConfigChanged()
    {
        var (vm, emitted) = Build(AppConfig.Default, captured: null);

        vm.Shortcuts.NextSlot.ClearCommand.Execute(null);

        Assert.Null(vm.Config.NextBinding);
        Assert.Equal("non assignée", vm.Shortcuts.NextSlot.Display);
        Assert.Single(emitted);
    }

    [Fact]
    public void ActivationDirecte_EstLieeAuCompte_EtMaterialiseeSurLeCompte()
    {
        // Un slot direct par compte ayant un personnage lié ; l'entrée est portée par le COMPTE (Axe 8).
        var f1 = new Binding(BindingKind.Key, 0x70);
        var config = AppConfig.Default with
        {
            NextBinding = null,
            PrevBinding = null,
            GameAccounts = [new GameAccount("Compte1")],
            Accounts = [new AccountConfig("Iop-Kamar", AccountName: "Compte1")],
        };
        var (vm, emitted) = Build(config, f1);

        var slot = Assert.Single(vm.Shortcuts.DirectSlots);
        Assert.Equal("Compte1", slot.Label); // personnage non connecté → repli sur le nom du compte

        slot.CaptureCommand.Execute(null);

        Assert.Equal(f1, vm.Config.GameAccounts[0].DirectBinding); // portée par le compte
        Assert.Null(vm.Config.Accounts[0].DirectBinding);          // pas par le personnage
        Assert.Equal("F1", slot.Display);
        Assert.NotEmpty(emitted);
    }

    [Fact]
    public void ActivationDirecte_AfficheLePersonnageConnecteDuCompte()
    {
        var config = AppConfig.Default with
        {
            GameAccounts = [new GameAccount("Compte1")],
            Accounts = [new AccountConfig("Iop", AccountName: "Compte1")],
        };
        var detector = new FakeWindowDetector();
        var vm = new MainViewModel(config, detector, new FakeInputCaptureService(null),
            new FakeStartupRegistryService(), new FakeFileDialogService());

        Assert.Equal("Compte1", Assert.Single(vm.Shortcuts.DirectSlots).Label); // aucun connecté → nom du compte

        detector.RaiseAppeared("Iop", 1);

        Assert.Equal("Iop", Assert.Single(vm.Shortcuts.DirectSlots).Label); // connecté → nom du personnage
    }

    [Fact]
    public void ActivationDirecte_CompteSansPersonnageLie_NAffichePasDeSlot()
    {
        var config = AppConfig.Default with
        {
            GameAccounts = [new GameAccount("Compte1")],
            Accounts = [new AccountConfig("Iop-Kamar")], // non lié
        };
        var (vm, _) = Build(config, null);

        Assert.Empty(vm.Shortcuts.DirectSlots);
    }

    [Fact]
    public void ActivationDirecte_EnConflitAvecSuivant_EstRefusee()
    {
        var config = AppConfig.Default with
        {
            PrevBinding = null,
            GameAccounts = [new GameAccount("Compte1")],
            Accounts = [new AccountConfig("Iop-Kamar", AccountName: "Compte1")],
        };
        var (vm, emitted) = Build(config, XButton2); // = suivant

        var slot = vm.Shortcuts.DirectSlots[0];
        slot.CaptureCommand.Execute(null);

        Assert.Null(vm.Config.GameAccounts[0].DirectBinding);
        Assert.NotNull(slot.ConflictMessage);
        Assert.Contains("Compte suivant", slot.ConflictMessage);
        Assert.Empty(emitted);
    }
}
