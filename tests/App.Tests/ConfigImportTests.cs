using System.IO;
using DofusSwitcher.Models;
using DofusSwitcher.Persistence;

// [WARN] App référence WinForms : Binding est ambigu. Alias explicite.
using Binding = DofusSwitcher.Models.Binding;

namespace DofusSwitcher.Tests;

/// <summary>
/// Tests de l'analyse/validation d'import (US-P04, RG-P07) : round-trip export→import, rejet des fichiers
/// illisibles / de schéma trop récent / en conflit, et migration montante d'un schéma antérieur.
/// [DECISION] L'import est pur (aucun effet de bord) : un fichier invalide est refusé, jamais appliqué.
/// </summary>
public class ConfigImportTests
{
    private static readonly Binding KeyA = new(BindingKind.Key, 0x41);

    /// <summary>Sérialise une config sur un fichier temporaire via le store standard, puis renvoie son contenu JSON.</summary>
    private static string SerializeToJson(AppConfig config)
    {
        var path = Path.Combine(Path.GetTempPath(), $"dofus-import-{Guid.NewGuid():N}.json");
        try
        {
            new JsonConfigStore(path).Save(config);
            return File.ReadAllText(path);
        }
        finally
        {
            File.Delete(path);
        }
    }

    [Fact]
    public void RoundTrip_ExportPuisImport_PreserveLaConfig()
    {
        var config = AppConfig.Default with
        {
            InterceptionSuspended = true,
            StartWithWindows = true,
            NextBinding = KeyA,
            Accounts = [new AccountConfig("Iop-Kamar", Excluded: true, DirectBinding: null)],
        };

        var json = SerializeToJson(config);
        var ok = ConfigImport.TryParse(json, out var parsed, out var error);

        Assert.True(ok);
        Assert.Null(error);
        Assert.NotNull(parsed);
        Assert.Equal(AppConfig.CurrentSchemaVersion, parsed!.SchemaVersion);
        Assert.True(parsed.InterceptionSuspended);
        Assert.True(parsed.StartWithWindows);
        Assert.Equal(KeyA, parsed.NextBinding);
        Assert.Equal(config.PrevBinding, parsed.PrevBinding);
        var account = Assert.Single(parsed.Accounts);
        Assert.Equal("Iop-Kamar", account.CharacterName);
        Assert.True(account.Excluded);
    }

    [Fact]
    public void FichierIllisible_EstRefuse_SansConfig()
    {
        var ok = ConfigImport.TryParse("{ ceci n'est pas du json", out var parsed, out var error);

        Assert.False(ok);
        Assert.Null(parsed);
        Assert.NotNull(error);
    }

    [Fact]
    public void SchemaTropRecent_EstRefuse()
    {
        var future = AppConfig.Default with { SchemaVersion = AppConfig.CurrentSchemaVersion + 1 };
        var json = SerializeToJson(future);

        var ok = ConfigImport.TryParse(json, out var parsed, out var error);

        Assert.False(ok);
        Assert.Null(parsed);
        Assert.NotNull(error);
    }

    [Fact]
    public void SchemaAnterieur_EstMigreMonte()
    {
        var old = AppConfig.Default with { SchemaVersion = AppConfig.CurrentSchemaVersion - 1 };
        var json = SerializeToJson(old);

        var ok = ConfigImport.TryParse(json, out var parsed, out _);

        Assert.True(ok);
        Assert.Equal(AppConfig.CurrentSchemaVersion, parsed!.SchemaVersion);
    }

    [Fact]
    public void ConflitDEntrees_EstRefuse()
    {
        // Même entrée pour suivant et précédent → viole l'unicité globale (data-model §Invariants).
        var conflicting = AppConfig.Default with { PrevBinding = AppConfig.Default.NextBinding };
        var json = SerializeToJson(conflicting);

        var ok = ConfigImport.TryParse(json, out var parsed, out var error);

        Assert.False(ok);
        Assert.Null(parsed);
        Assert.NotNull(error);
    }
}
