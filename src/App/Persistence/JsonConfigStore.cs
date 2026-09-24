using System.IO;
using System.Text.Json;
using DofusSwitcher.Constants;
using DofusSwitcher.Models;

namespace DofusSwitcher.Persistence;

/// <summary>
/// Implémentation JSON de <see cref="IConfigStore"/> : écriture atomique, reprise sur corruption,
/// migration montante par <c>schemaVersion</c> (archi §Persistance).
/// </summary>
public sealed class JsonConfigStore : IConfigStore
{
    private readonly string _path;

    /// <summary>Crée un store pointant sur le fichier de config standard (<c>%APPDATA%</c>).</summary>
    public JsonConfigStore() : this(AppConstants.ConfigFilePath) { }

    /// <summary>
    /// Crée un store sur un chemin explicite.
    /// [DECISION] Chemin injectable pour rendre la persistance testable (répertoire temporaire)
    /// sans toucher au vrai <c>%APPDATA%</c> ; le ctor sans argument reste le cas de production.
    /// </summary>
    public JsonConfigStore(string path) => _path = path;

    /// <inheritdoc/>
    public AppConfig Load()
    {
        if (!File.Exists(_path)) return AppConfig.Default;

        AppConfig? config;
        try
        {
            var json = File.ReadAllText(_path);
            config = JsonSerializer.Deserialize(json, AppJsonContext.Default.AppConfig);
        }
        catch (Exception ex) when (ex is JsonException or IOException)
        {
            // Fichier illisible/corrompu : conserver une copie fautive puis repartir vide (RG-P06).
            TryBackupCorrupt();
            return AppConfig.Default;
        }

        // Désérialisation en null (ex. contenu "null") ou schéma d'une app plus récente :
        // traité comme illisible — jamais de perte silencieuse (data-model §Invariants).
        if (config is null || config.SchemaVersion > AppConfig.CurrentSchemaVersion)
        {
            TryBackupCorrupt();
            return AppConfig.Default;
        }

        // Schéma antérieur : migration montante idempotente avant usage.
        return config.SchemaVersion < AppConfig.CurrentSchemaVersion ? Migrate(config) : config;
    }

    /// <inheritdoc/>
    public void Save(AppConfig config)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(_path)!);

        // [WARN] Écriture atomique : écrire dans un temp puis remplacer, pour ne jamais laisser
        // le fichier de config à moitié écrit si le process meurt en cours d'écriture (RG-P05).
        var tmp = _path + ".tmp";
        File.WriteAllText(tmp, JsonSerializer.Serialize(config, AppJsonContext.Default.AppConfig));
        File.Move(tmp, _path, overwrite: true);
    }

    /// <summary>
    /// Migration montante idempotente, partagée avec l'import (<see cref="ConfigImport"/>).
    /// v1→v2 : ajout des comptes (<see cref="AppConfig.GameAccounts"/>) et du lien
    /// <see cref="AccountConfig.AccountName"/>. Une config v1 n'a pas le champ <c>gameAccounts</c>
    /// (désérialisé <c>null</c>) → normalisé en liste vide ; les personnages restent non liés
    /// (<c>accountName = null</c>). Les évolutions futures ajouteront ici leurs transformations.
    /// </summary>
    internal static AppConfig Migrate(AppConfig config) =>
        config with
        {
            SchemaVersion = AppConfig.CurrentSchemaVersion,
            GameAccounts = config.GameAccounts ?? [],
        };

    /// <summary>
    /// Copie best-effort du fichier fautif en <c>*.corrupt-&lt;horodatage&gt;</c> avant de repartir
    /// sur une config par défaut. Un échec de copie ne doit jamais empêcher le démarrage.
    /// </summary>
    private void TryBackupCorrupt()
    {
        try
        {
            var stamp = DateTime.Now.ToString("yyyyMMdd-HHmmss");
            File.Copy(_path, $"{_path}.corrupt-{stamp}", overwrite: true);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // Sauvegarde best-effort : on avale l'échec pour garantir un démarrage sans crash.
        }
    }
}
