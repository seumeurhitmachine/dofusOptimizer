using System.IO;
using System.Text.RegularExpressions;
using DofusSwitcher.Models;
using DofusSwitcher.Persistence;

// [WARN] App référence WinForms : ses global usings exposent System.Windows.Forms.Binding.
// Alias vers notre value object pour lever l'ambiguïté (CS0104).
using Binding = DofusSwitcher.Models.Binding;

namespace DofusSwitcher.Tests;

/// <summary>
/// Tests de <see cref="JsonConfigStore"/> : round-trip (CA-04), reprise sur corruption (RG-P06),
/// migration <c>schemaVersion</c>, chemin absent. Chaque test s'isole dans un répertoire temporaire
/// via le ctor à chemin explicite.
/// </summary>
public sealed class ConfigStoreTests : IDisposable
{
    private readonly string _dir;
    private readonly string _path;

    public ConfigStoreTests()
    {
        _dir = Path.Combine(Path.GetTempPath(), "DofusSwitcherTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_dir);
        _path = Path.Combine(_dir, "config.json");
    }

    public void Dispose()
    {
        try { Directory.Delete(_dir, recursive: true); } catch (IOException) { }
    }

    [Fact]
    public void Load_FichierAbsent_RetourneDefaut()
    {
        var store = new JsonConfigStore(_path);

        var config = store.Load();

        AssertConfigEqual(AppConfig.Default, config);
        Assert.False(File.Exists(_path)); // Load ne crée rien.
    }

    [Fact]
    public void SaveReload_PreserveOrdreEtAssociations()
    {
        var store = new JsonConfigStore(_path);
        var original = AppConfig.Default with
        {
            InterceptionSuspended = true,
            Accounts =
            [
                new AccountConfig("Enutrof", Excluded: false, DirectBinding: new Binding(BindingKind.Key, 112)),
                new AccountConfig("Sacrieur", Excluded: true),
                new AccountConfig("Osamodas"),
            ],
        };

        store.Save(original);
        var reloaded = new JsonConfigStore(_path).Load();

        AssertConfigEqual(original, reloaded); // ordre + associations identiques (CA-04).
        Assert.Equal(
            new[] { "Enutrof", "Sacrieur", "Osamodas" },
            reloaded.Accounts.Select(a => a.CharacterName));
    }

    [Fact]
    public void Save_EcritDuJsonLisibleCamelCaseAvecEnumsEnChaines()
    {
        var store = new JsonConfigStore(_path);

        store.Save(AppConfig.Default);
        var json = File.ReadAllText(_path);

        Assert.Contains("\"schemaVersion\"", json);        // camelCase
        Assert.Contains("\"MouseButton\"", json);          // enum sérialisé en chaîne
        Assert.Contains("\n", json);                        // indenté (lisible)
    }

    [Fact]
    public void Load_FichierCorrompu_SauvegardeCopieEtRetourneDefaut()
    {
        File.WriteAllText(_path, "{ ceci n'est pas du json valide");

        var config = new JsonConfigStore(_path).Load();

        AssertConfigEqual(AppConfig.Default, config);
        Assert.NotEmpty(BackupsIn(_dir)); // copie *.corrupt-<horodatage> conservée.
    }

    [Fact]
    public void Load_SchemaTropRecent_TraiteCommeIllisible()
    {
        WriteDefaultWithSchemaVersion(AppConfig.CurrentSchemaVersion + 1);

        var config = new JsonConfigStore(_path).Load();

        AssertConfigEqual(AppConfig.Default, config);
        Assert.NotEmpty(BackupsIn(_dir));
    }

    [Fact]
    public void Load_SchemaAnterieur_MigreVersLaVersionCourante()
    {
        // Document de schéma antérieur (0) mais par ailleurs valide.
        WriteDefaultWithSchemaVersion(0);

        var config = new JsonConfigStore(_path).Load();

        Assert.Equal(AppConfig.CurrentSchemaVersion, config.SchemaVersion);
        Assert.Empty(BackupsIn(_dir)); // migration silencieuse, pas une corruption.
    }

    /// <summary>
    /// Écrit un config.json valide via le store, puis force sa <c>schemaVersion</c> — permet de
    /// fabriquer des documents de schéma antérieur/postérieur sans dépendre du contexte interne.
    /// </summary>
    private void WriteDefaultWithSchemaVersion(int version)
    {
        new JsonConfigStore(_path).Save(AppConfig.Default);
        var patched = Regex.Replace(
            File.ReadAllText(_path),
            "\"schemaVersion\": \\d+",
            $"\"schemaVersion\": {version}");
        File.WriteAllText(_path, patched);
    }

    /// <summary>
    /// Compare deux configs par valeur. Nécessaire car <see cref="AppConfig"/> est un record dont
    /// le champ <c>Accounts</c> (une <c>List</c>) est comparé par référence par l'égalité générée —
    /// on compare donc la séquence des comptes explicitement (égalité de valeur élément par élément).
    /// </summary>
    private static void AssertConfigEqual(AppConfig expected, AppConfig actual)
    {
        Assert.Equal(expected.SchemaVersion, actual.SchemaVersion);
        Assert.Equal(expected.InterceptionSuspended, actual.InterceptionSuspended);
        Assert.Equal(expected.StartWithWindows, actual.StartWithWindows);
        Assert.Equal(expected.NextBinding, actual.NextBinding);
        Assert.Equal(expected.PrevBinding, actual.PrevBinding);
        Assert.Equal(expected.Accounts, actual.Accounts);
    }

    private static string[] BackupsIn(string dir) =>
        Directory.GetFiles(dir, "*.corrupt-*");
}
