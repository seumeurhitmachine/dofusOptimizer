using System.IO;
using DofusSwitcher.Models;
using DofusSwitcher.Persistence;
using DofusSwitcher.ViewModels;

// [WARN] App référence WinForms : Binding est ambigu. Alias explicite.
using Binding = DofusSwitcher.Models.Binding;

namespace DofusSwitcher.Tests;

/// <summary>
/// Tests de l'onglet Réglages (Axe 7) au travers du <see cref="MainViewModel"/> (seul writer) : suspension
/// persistée (RG-T02), démarrage Windows fakable (RG-T05), export/import de config (US-P04).
/// </summary>
public class SettingsViewModelTests
{
    private static readonly Binding KeyA = new(BindingKind.Key, 0x41);

    private static (MainViewModel vm, List<AppConfig> emitted, FakeStartupRegistryService startup) Build(
        AppConfig config, FakeFileDialogService? fileDialog = null)
    {
        var startup = new FakeStartupRegistryService();
        var vm = new MainViewModel(config, new FakeWindowDetector(), new FakeInputCaptureService(null),
            startup, fileDialog ?? new FakeFileDialogService());
        var emitted = new List<AppConfig>();
        vm.ConfigChanged += emitted.Add;
        return (vm, emitted, startup);
    }

    private static string WriteTempConfig(AppConfig config)
    {
        var path = Path.Combine(Path.GetTempPath(), $"dofus-settings-{Guid.NewGuid():N}.json");
        new JsonConfigStore(path).Save(config);
        return path;
    }

    [Fact]
    public void BasculeSuspension_PersisteEtEmetConfigChanged()
    {
        var (vm, emitted, _) = Build(AppConfig.Default);

        vm.Settings.IsInterceptionSuspended = true;

        Assert.True(vm.Config.InterceptionSuspended);
        Assert.Single(emitted);
        Assert.True(emitted[0].InterceptionSuspended);
    }

    [Fact]
    public void BasculeDemarrageWindows_AppelleLeService_EtPersiste()
    {
        var (vm, emitted, startup) = Build(AppConfig.Default);

        vm.Settings.StartWithWindows = true;

        Assert.True(startup.IsEnabled());
        Assert.Equal(1, startup.SetCount);
        Assert.True(vm.Config.StartWithWindows);
        Assert.Single(emitted);
        Assert.True(emitted[0].StartWithWindows);
    }

    [Fact]
    public void Export_EcritUnFichierReimportable()
    {
        var savePath = Path.Combine(Path.GetTempPath(), $"dofus-export-{Guid.NewGuid():N}.json");
        var config = AppConfig.Default with { InterceptionSuspended = true, NextBinding = KeyA };
        var (vm, _, _) = Build(config, new FakeFileDialogService(savePath: savePath));

        try
        {
            vm.Settings.ExportCommand.Execute(null);

            Assert.True(File.Exists(savePath));
            Assert.False(vm.Settings.IsStatusError);

            var ok = ConfigImport.TryParse(File.ReadAllText(savePath), out var parsed, out _);
            Assert.True(ok);
            Assert.True(parsed!.InterceptionSuspended);
            Assert.Equal(KeyA, parsed.NextBinding);
        }
        finally
        {
            File.Delete(savePath);
        }
    }

    [Fact]
    public void Import_AppliqueLaConfig_EtRechargeComptesEtRaccourcis()
    {
        var imported = AppConfig.Default with
        {
            NextBinding = KeyA,
            Accounts = [new AccountConfig("Iop-Kamar"), new AccountConfig("Cra-Lena")],
        };
        var path = WriteTempConfig(imported);
        var (vm, emitted, _) = Build(AppConfig.Default, new FakeFileDialogService(openPath: path));

        try
        {
            vm.Settings.ImportCommand.Execute(null);

            Assert.False(vm.Settings.IsStatusError);
            Assert.Equal(KeyA, vm.Config.NextBinding);
            Assert.Equal(2, vm.Config.Accounts.Count);
            Assert.Equal(2, vm.Accounts.Items.Count);      // personnages rechargés
            Assert.Empty(vm.Shortcuts.DirectSlots);        // personnages importés non liés → aucun slot direct (Axe 8)
            Assert.NotEmpty(emitted);                        // persisté (autosave)
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void Import_FichierInvalide_RefuseEtPreserveLaConfig()
    {
        var path = Path.Combine(Path.GetTempPath(), $"dofus-bad-{Guid.NewGuid():N}.json");
        File.WriteAllText(path, "{ pas une config valide");
        var (vm, emitted, _) = Build(AppConfig.Default, new FakeFileDialogService(openPath: path));

        try
        {
            vm.Settings.ImportCommand.Execute(null);

            Assert.True(vm.Settings.IsStatusError);
            Assert.NotNull(vm.Settings.StatusMessage);
            Assert.Empty(vm.Config.Accounts); // config par défaut inchangée
            Assert.Empty(emitted);            // aucune persistance
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void Import_Annule_NeChangeRien()
    {
        var (vm, emitted, _) = Build(AppConfig.Default, new FakeFileDialogService(openPath: null));

        vm.Settings.ImportCommand.Execute(null);

        Assert.Null(vm.Settings.StatusMessage);
        Assert.Empty(emitted);
    }
}
