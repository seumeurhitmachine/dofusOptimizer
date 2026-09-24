using DofusSwitcher.Constants;
using DofusSwitcher.Models;

// [WARN] App référence WinForms : ses global usings exposent System.Windows.Forms.Binding.
// Alias vers notre value object pour lever l'ambiguïté (CS0104).
using Binding = DofusSwitcher.Models.Binding;

namespace DofusSwitcher.Tests;

/// <summary>
/// Tests du modèle de config : défauts, égalité de valeur des <see cref="Binding"/> et invariant
/// d'unicité globale (data-model §Invariants).
/// </summary>
public class AppConfigTests
{
    [Fact]
    public void Default_UtiliseXButton2EtXButton1()
    {
        var config = AppConfig.Default;

        Assert.Equal(AppConfig.CurrentSchemaVersion, config.SchemaVersion);
        Assert.Empty(config.Accounts);
        Assert.Equal(new Binding(BindingKind.MouseButton, AppConstants.DefaultNextButtonCode), config.NextBinding);
        Assert.Equal(new Binding(BindingKind.MouseButton, AppConstants.DefaultPrevButtonCode), config.PrevBinding);
    }

    [Fact]
    public void Binding_EgaliteParKindEtCode()
    {
        Assert.Equal(new Binding(BindingKind.Key, 65), new Binding(BindingKind.Key, 65));
        Assert.NotEqual(new Binding(BindingKind.Key, 65), new Binding(BindingKind.MouseButton, 65));
        Assert.NotEqual(new Binding(BindingKind.Key, 65), new Binding(BindingKind.Key, 66));
    }

    [Fact]
    public void HasBindingConflicts_FauxParDefaut()
    {
        Assert.False(AppConfig.Default.HasBindingConflicts());
    }

    [Fact]
    public void HasBindingConflicts_VraiQuandUneDirecteReprendUneGlobale()
    {
        var config = AppConfig.Default with
        {
            Accounts = [new AccountConfig("Iop", DirectBinding: new Binding(BindingKind.MouseButton, AppConstants.DefaultNextButtonCode))],
        };

        Assert.True(config.HasBindingConflicts());
    }

    [Fact]
    public void HasBindingConflicts_VraiQuandDeuxComptesPartagentUneDirecte()
    {
        var shared = new Binding(BindingKind.Key, 112); // F1
        var config = new AppConfig(
            SchemaVersion: AppConfig.CurrentSchemaVersion,
            InterceptionSuspended: false,
            StartWithWindows: false,
            NextBinding: null,
            PrevBinding: null,
            Accounts:
            [
                new AccountConfig("Iop", DirectBinding: shared),
                new AccountConfig("Cra", DirectBinding: shared),
            ],
            GameAccounts: []);

        Assert.True(config.HasBindingConflicts());
    }
}
