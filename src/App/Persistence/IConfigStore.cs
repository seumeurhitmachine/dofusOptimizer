using DofusSwitcher.Models;

namespace DofusSwitcher.Persistence;

/// <summary>Charge et sauvegarde la configuration applicative persistée.</summary>
public interface IConfigStore
{
    /// <summary>
    /// Charge la configuration. Fichier absent → configuration par défaut. Fichier illisible,
    /// corrompu ou de schéma trop récent → copie <c>*.corrupt-&lt;horodatage&gt;</c> conservée puis
    /// configuration par défaut (jamais de crash, RG-P06). Schéma inférieur → migration montante.
    /// </summary>
    AppConfig Load();

    /// <summary>Sauvegarde la configuration de façon atomique (jamais de fichier tronqué, RG-P05).</summary>
    void Save(AppConfig config);
}
