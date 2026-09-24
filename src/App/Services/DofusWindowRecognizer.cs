namespace DofusSwitcher.Services;

/// <summary>
/// Reconnaissance d'une fenêtre de client DOFUS et extraction du nom de personnage, à partir des
/// seules données de fenêtre <c>user32</c> (titre, classe).
/// [ARCH] C-02 / PO-001 : AUCUNE inspection du processus (pas de GetWindowThreadProcessId, pas
/// d'OpenProcess). L'identité vient du titre (H-01) — plusieurs processus Dofus.exe coexistent,
/// mais le titre porte le nom du personnage.
/// [DECISION] Le critère se fonde sur le motif de titre <c>" - Dofus"</c> (porteur d'identité, H-01) ;
/// le nom de classe est accepté comme signal secondaire relâché (non requis), car il varie selon la
/// techno du client. Isoler le critère et l'extraction en fonctions pures rend le risque de changement
/// de format de titre testable indépendamment (spec §3.x).
/// </summary>
public static class DofusWindowRecognizer
{
    /// <summary>
    /// Séparateur entre le nom de personnage et le reste du titre DOFUS
    /// (ex. « Iop-Kamar - Dofus 2.68 »). Sert de motif de reconnaissance et de point de coupe.
    /// </summary>
    private const string TitleMarker = " - Dofus";

    /// <summary>
    /// Vrai si la fenêtre est reconnue comme un client DOFUS porteur d'un nom de personnage,
    /// d'après son <paramref name="title"/> et, à titre secondaire, sa <paramref name="className"/>.
    /// Un titre sans nom extractible (marqueur seul, nom vide) n'est pas retenu.
    /// </summary>
    public static bool IsDofusWindow(string? title, string? className)
        => ExtractCharacterName(title) is not null;

    /// <summary>
    /// Extrait le nom de personnage du <paramref name="title"/> d'une fenêtre DOFUS.
    /// Fonction pure : retourne le segment précédant <c>" - Dofus"</c>, nettoyé, ou <c>null</c> si le
    /// titre ne correspond pas au format ou si le nom est vide (RG-D01).
    /// </summary>
    public static string? ExtractCharacterName(string? title)
    {
        if (string.IsNullOrWhiteSpace(title)) return null;

        var markerIndex = title.IndexOf(TitleMarker, StringComparison.OrdinalIgnoreCase);
        if (markerIndex <= 0) return null; // marqueur absent (-1) ou en tête (nom vide, 0)

        var name = title[..markerIndex].Trim();
        return name.Length == 0 ? null : name;
    }
}
