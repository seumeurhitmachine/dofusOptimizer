using DofusSwitcher.Models;
using DofusSwitcher.Services;
using DofusSwitcher.ViewModels;

// [WARN] App référence WinForms : Binding est ambigu. Alias explicite.
using Binding = DofusSwitcher.Models.Binding;

namespace DofusSwitcher.Tests;

/// <summary>
/// Tests de l'Axe 11 : clients DOFUS connectés <b>sans personnage</b> (écran de sélection) comme participants
/// runtime de la rotation (« Dofus N », fermables, jamais persistés, sans compte ni raccourci) ; « Ouvrir une
/// session » proposé tant qu'aucun personnage n'est connecté ; réduction de l'app à l'ouverture d'une session.
/// </summary>
public class AccountsAxe11Tests
{
    private static MainViewModel BuildVm(AppConfig config, FakeWindowDetector detector,
        FakeSessionProcessService? session = null, Action? requestMinimize = null) =>
        new(config, detector, new FakeInputCaptureService(null), new FakeStartupRegistryService(),
            new FakeFileDialogService(), session, null, requestMinimize);

    // ---- Fusion : client sans personnage = runtime-only ----

    [Fact]
    public void AccountMerge_ClientSansPersonnage_EstAjouteConnecteEtSansPersonnage()
    {
        var merged = AccountMerge.Merge([], [new DetectedWindow("\0dofus:5", 5, HasCharacter: false)]);

        var state = Assert.Single(merged);
        Assert.True(state.IsConnected);
        Assert.False(state.HasCharacter);
        Assert.Null(state.AccountName);
    }

    // ---- VM : ligne « Dofus N », exclue des raccourcis / de la liaison ----

    [Fact]
    public void ClientsSansPersonnage_ApparaissentEnLignesAnonymesNumerotees()
    {
        var detector = new FakeWindowDetector();
        var vm = BuildVm(AppConfig.Default, detector);

        detector.RaiseAnonymousAppeared(10);
        detector.RaiseAnonymousAppeared(11);

        var rows = vm.Accounts.ConnectedRows.Where(r => r.IsAnonymous).ToList();
        Assert.Equal(2, rows.Count);
        Assert.Equal(new[] { "Dofus 1", "Dofus 2" }, rows.Select(r => r.Title));
    }

    [Fact]
    public void ClientSansPersonnage_NestNiLiableNiRaccourcissable()
    {
        var config = AppConfig.Default with { GameAccounts = [new GameAccount("Compte1")] };
        var detector = new FakeWindowDetector();
        var vm = BuildVm(config, detector);

        detector.RaiseAnonymousAppeared(10);

        Assert.Empty(vm.Accounts.ConnectedUnlinkedCharacters()); // aucun slot direct « sans compte »
        Assert.Empty(vm.Shortcuts.DirectSlots);                  // aucun raccourci
        var row = Assert.Single(vm.Accounts.ConnectedRows);
        Assert.True(row.IsAnonymous);
        Assert.False(row.IsLinked);
    }

    // ---- Rotation : inclus, sans activation directe ----

    [Fact]
    public void ClientSansPersonnage_EstDansLaRotation_SansActivationDirecte()
    {
        var detector = new FakeWindowDetector();
        var vm = BuildVm(AppConfig.Default with { NextBinding = null, PrevBinding = null }, detector);

        detector.RaiseAnonymousAppeared(77);

        var snap = vm.BuildRotationSnapshot();
        var slot = Assert.Single(snap.Slots);
        Assert.Equal((nint)77, slot.Handle);
        Assert.True(slot.IsRotatable);
        Assert.Empty(snap.Directs);
    }

    // ---- Jamais persisté ----

    [Fact]
    public void ClientSansPersonnage_NestJamaisPersiste_MemeApresActionSurUnPerso()
    {
        var detector = new FakeWindowDetector();
        var vm = BuildVm(AppConfig.Default, detector);
        detector.RaiseAppeared("Iop", 1);   // personnage réel (runtime)
        detector.RaiseAnonymousAppeared(2); // client sans personnage

        // Une action de persistance sur le personnage réel matérialise l'ordre visible.
        vm.Accounts.ToggleExcludeItemCommand.Execute(vm.Accounts.Items.Single(i => i.HasCharacter));

        var persisted = Assert.Single(vm.Config.Accounts);
        Assert.Equal("Iop", persisted.CharacterName);
        Assert.DoesNotContain(vm.Config.Accounts, a => a.CharacterName.StartsWith('\0')); // aucun anonyme
    }

    // ---- Gating « Ouvrir une session » (Axe 11) ----

    [Fact]
    public void ClientSansPersonnageOuvert_OuvrirSessionMasque()
    {
        var session = new FakeSessionProcessService { LauncherRunning = true };
        var detector = new FakeWindowDetector();
        var vm = BuildVm(AppConfig.Default, detector, session);

        detector.RaiseAnonymousAppeared(3);

        Assert.False(vm.Accounts.ShowOpenSession);      // une fenêtre DOFUS est ouverte → masqué (Axe 11)
        Assert.True(vm.Accounts.HasConnectedClients);   // fermable (croix / Terminer session)
    }

    [Fact]
    public void PersonnageConnecte_OuvrirSessionMasque()
    {
        var session = new FakeSessionProcessService();
        var detector = new FakeWindowDetector();
        var vm = BuildVm(AppConfig.Default, detector, session);

        detector.RaiseAppeared("Iop", 1);

        Assert.False(vm.Accounts.ShowOpenSession);
        Assert.True(vm.Accounts.HasConnectedClients);
    }

    [Fact]
    public void AucuneFenetreDofus_LauncherOuvert_OuvrirSessionRestePropose()
    {
        // Launcher lancé mais aucun client DOFUS encore ouvert : on propose toujours (l'action activera sa fenêtre).
        var session = new FakeSessionProcessService { LauncherRunning = true };
        var vm = BuildVm(AppConfig.Default, new FakeWindowDetector(), session);

        Assert.True(vm.Accounts.ShowOpenSession);
    }

    // ---- Réduction de l'app à l'ouverture d'une session ----

    [Fact]
    public void OuvrirSession_AvecOptionReduire_LanceEtMinimise()
    {
        var minimizeCalls = 0;
        var session = new FakeSessionProcessService();
        var vm = BuildVm(AppConfig.Default with { MinimizeOnOpenSession = true }, new FakeWindowDetector(),
            session, () => minimizeCalls++);

        vm.Accounts.OpenSessionCommand.Execute(null);

        Assert.Equal(1, session.LaunchCount);
        Assert.Equal(1, minimizeCalls);
    }

    [Fact]
    public void OuvrirSession_SansOptionReduire_NeMinimisePas()
    {
        var minimizeCalls = 0;
        var session = new FakeSessionProcessService(); // MinimizeOnOpenSession = false (défaut)
        var vm = BuildVm(AppConfig.Default, new FakeWindowDetector(), session, () => minimizeCalls++);

        vm.Accounts.OpenSessionCommand.Execute(null);

        Assert.Equal(0, minimizeCalls);
    }

    [Fact]
    public void OuvrirSession_LauncherDejaOuvert_ActiveSaFenetreSansRelancer()
    {
        var session = new FakeSessionProcessService { LauncherRunning = true };
        var vm = BuildVm(AppConfig.Default, new FakeWindowDetector(), session);

        vm.Accounts.OpenSessionCommand.Execute(null);

        Assert.Equal(1, session.ActivateCount);
        Assert.Equal(0, session.LaunchCount);
    }
}
