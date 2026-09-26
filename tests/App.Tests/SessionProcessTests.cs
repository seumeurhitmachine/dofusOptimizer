using DofusSwitcher.Models;
using DofusSwitcher.Services;
using DofusSwitcher.ViewModels;

namespace DofusSwitcher.Tests;

/// <summary>
/// Cycle de session (Axe 9) : résolution pure du chemin du launcher, gating du bouton « Ouvrir une
/// session », force-kill par ligne et « Terminer session » (kill de tous les clients + fermeture de l'app).
/// L'exception C-02 (handles process) est isolée dans <see cref="ISessionProcessService"/> — ici, un fake.
/// </summary>
public class SessionProcessTests
{
    // --- Résolution pure du chemin (helper testable sans disque). ---

    [Fact]
    public void ResolveLauncherPath_AucunCandidatExistant_RenvoieNull()
    {
        var result = SessionProcessService.ResolveLauncherPath(["a.exe", "b.exe"], _ => false);
        Assert.Null(result);
    }

    [Fact]
    public void ResolveLauncherPath_PremierExistant_EstPrefere()
    {
        var result = SessionProcessService.ResolveLauncherPath(
            [@"local\Ankama Launcher.exe", @"reg\Ankama Launcher.exe"],
            p => p.StartsWith("reg"));

        Assert.Equal(@"reg\Ankama Launcher.exe", result);
    }

    // --- Gating du bouton « Ouvrir une session ». ---

    [Fact]
    public void SansServiceSession_BoutonOuvrirMasque()
    {
        var vm = new AccountsViewModel([], new FakeWindowDetector());
        Assert.False(vm.ShowOpenSession);
    }

    [Fact]
    public void AucunClient_LauncherAbsent_BoutonOuvrirVisible()
    {
        var session = new FakeSessionProcessService { LauncherRunning = false };
        var vm = new AccountsViewModel([], new FakeWindowDetector(), null, session);

        Assert.True(vm.ShowOpenSession);
        Assert.True(vm.OpenSessionCommand.CanExecute(null)); // chemin résolu
    }

    [Fact]
    public void LauncherOuvert_BoutonToujoursVisible_EtActiveLaFenetre()
    {
        // Axe 11 : « Ouvrir une session » est proposé tant qu'aucun PERSONNAGE n'est connecté — même launcher
        // déjà ouvert. Dans ce cas, l'action ramène la fenêtre du launcher au premier plan (pas de relance).
        var session = new FakeSessionProcessService { LauncherRunning = true };
        var vm = new AccountsViewModel([], new FakeWindowDetector(), null, session);

        Assert.True(vm.ShowOpenSession);

        vm.OpenSessionCommand.Execute(null);

        Assert.Equal(1, session.ActivateCount);
        Assert.Equal(0, session.LaunchCount);
    }

    [Fact]
    public void ClientConnecte_BoutonOuvrirMasque()
    {
        var session = new FakeSessionProcessService { LauncherRunning = false };
        var detector = new FakeWindowDetector();
        var vm = new AccountsViewModel([], detector, null, session);

        detector.RaiseAppeared("Iop-Kamar", 42);

        Assert.False(vm.ShowOpenSession);
        Assert.True(vm.HasConnectedClients);
    }

    [Fact]
    public void CheminIntrouvable_BoutonOuvrirMasque()
    {
        var session = new FakeSessionProcessService { LauncherPath = null };
        var vm = new AccountsViewModel([], new FakeWindowDetector(), null, session);

        Assert.False(vm.ShowOpenSession);                     // masqué : aucun chemin de launcher utilisable
        Assert.False(vm.OpenSessionCommand.CanExecute(null));
    }

    [Fact]
    public void CheminConfigure_Prime_EtRendLeBoutonVisible()
    {
        // Aucune auto-détection possible, mais un chemin est configuré dans les Réglages.
        var session = new FakeSessionProcessService { LauncherPath = null };
        var vm = new AccountsViewModel([], new FakeWindowDetector(), null, session);
        Assert.False(vm.ShowOpenSession);

        vm.SetLauncherPath(@"D:\Jeux\Ankama Launcher.exe");

        Assert.True(vm.ShowOpenSession);
        Assert.True(vm.OpenSessionCommand.CanExecute(null));

        vm.OpenSessionCommand.Execute(null);
        Assert.Equal(@"D:\Jeux\Ankama Launcher.exe", session.LastLaunchedPath); // le chemin configuré est lancé
    }

    [Fact]
    public void SetLauncherPathVide_RetombeSurAutoDetection()
    {
        var session = new FakeSessionProcessService { LauncherPath = @"C:\auto\Ankama Launcher.exe" };
        var vm = new AccountsViewModel([], new FakeWindowDetector(), null, session);

        vm.SetLauncherPath("   "); // vide/espaces → auto-détection
        vm.OpenSessionCommand.Execute(null);

        Assert.Equal(@"C:\auto\Ankama Launcher.exe", session.LastLaunchedPath);
    }

    // --- Lancement. ---

    [Fact]
    public void OuvrirSession_LauncherAbsent_LanceLeLauncher()
    {
        var session = new FakeSessionProcessService(); // LauncherRunning = false
        var vm = new AccountsViewModel([], new FakeWindowDetector(), null, session);

        vm.OpenSessionCommand.Execute(null);

        Assert.Equal(1, session.LaunchCount);
        Assert.Equal(0, session.ActivateCount); // launcher absent → lancement, pas d'activation
    }

    // --- Force-kill par ligne. ---

    [Fact]
    public void FermerClient_ForceKillLeHandleDeLaLigne()
    {
        var session = new FakeSessionProcessService();
        var detector = new FakeWindowDetector();
        var vm = new AccountsViewModel([], detector, null, session);
        detector.RaiseAppeared("Cra-Lena", 7);

        vm.CloseClientCommand.Execute(vm.Items[0]);

        Assert.Equal([7], session.KilledHandles);
    }

    // --- Terminer session. ---

    [Fact]
    public void TerminerSession_KillTousLesClientsConnectes_PuisFermeLApp()
    {
        var session = new FakeSessionProcessService();
        var detector = new FakeWindowDetector();
        var shutdownCalled = 0;
        var vm = new AccountsViewModel([], detector, null, session, () => shutdownCalled++);

        detector.RaiseAppeared("Iop-Kamar", 1);
        detector.RaiseAppeared("Cra-Lena", 2);

        Assert.True(vm.EndSessionCommand.CanExecute(null));
        vm.EndSessionCommand.Execute(null);

        Assert.Equal(2, session.KilledHandles.Count);
        Assert.Contains((nint)1, session.KilledHandles);
        Assert.Contains((nint)2, session.KilledHandles);
        Assert.Equal(1, shutdownCalled);
    }

    [Fact]
    public void TerminerSession_DesactiveSansClientConnecte()
    {
        var session = new FakeSessionProcessService();
        var vm = new AccountsViewModel([new AccountConfig("Iop-Kamar")], new FakeWindowDetector(), null, session);

        Assert.False(vm.HasConnectedClients); // persisté mais absent
        Assert.False(vm.EndSessionCommand.CanExecute(null));
    }
}
