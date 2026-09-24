using DofusSwitcher.Models;

namespace DofusSwitcher.Services;

/// <summary>
/// Sauvegarde débouncée de la configuration : chaque modification réarme un délai, l'écriture
/// disque n'a lieu qu'après un court silence (RG-P02, archi §Threading).
/// </summary>
public interface IConfigAutosave
{
    /// <summary>Signale une modification et fournit le dernier instantané à persister ; réarme le délai.</summary>
    void Notify(AppConfig config);

    /// <summary>Force l'écriture immédiate de l'instantané en attente, s'il y en a un (flush à la sortie).</summary>
    void SaveNow();
}
