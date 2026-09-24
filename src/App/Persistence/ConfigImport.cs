using System.Text.Json;
using DofusSwitcher.Models;

namespace DofusSwitcher.Persistence;

/// <summary>
/// Analyse et valide une configuration importée (US-P04, RG-P07) — foyer <b>pur</b> et testable.
/// [DECISION] Distinct de <see cref="JsonConfigStore.Load"/> : l'import ne doit jamais avoir d'effet de
/// bord (pas de backup, pas de repli silencieux sur la config par défaut qui effacerait le paramétrage
/// courant). Un fichier invalide est <b>refusé</b> avec un message, la config en place est préservée.
/// </summary>
public static class ConfigImport
{
    /// <summary>
    /// Tente de lire une config depuis du JSON. En cas de succès, <paramref name="config"/> est une config
    /// valide et sans conflit (migrée si son schéma était antérieur). En cas d'échec, <paramref name="error"/>
    /// porte un message lisible et <paramref name="config"/> est <c>null</c>.
    /// </summary>
    public static bool TryParse(string json, out AppConfig? config, out string? error)
    {
        config = null;
        error = null;

        AppConfig? parsed;
        try
        {
            parsed = JsonSerializer.Deserialize(json, AppJsonContext.Default.AppConfig);
        }
        catch (JsonException)
        {
            error = "Fichier illisible : ce n'est pas une configuration valide.";
            return false;
        }

        if (parsed is null)
        {
            error = "Fichier vide ou illisible.";
            return false;
        }

        if (parsed.SchemaVersion > AppConfig.CurrentSchemaVersion)
        {
            error = "Configuration créée par une version plus récente de l'application.";
            return false;
        }

        // Schéma antérieur → migration montante idempotente (même règle que le chargement normal).
        if (parsed.SchemaVersion < AppConfig.CurrentSchemaVersion)
            parsed = parsed with { SchemaVersion = AppConfig.CurrentSchemaVersion };

        // L'invariant d'unicité globale des entrées doit être préservé (data-model §Invariants) : une
        // config importée en conflit corromprait l'état — on refuse plutôt que d'appliquer.
        if (parsed.HasBindingConflicts())
        {
            error = "Configuration invalide : une même entrée est associée à plusieurs actions.";
            return false;
        }

        config = parsed;
        return true;
    }
}
