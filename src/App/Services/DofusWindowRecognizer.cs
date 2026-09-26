namespace DofusSwitcher.Services;

/// <summary>
/// Reconnaissance d'une fenêtre de client DOFUS (client Unity) et extraction du nom de personnage, à
/// partir des seules données de fenêtre <c>user32</c> (classe + titre).
/// [ARCH] C-02 / PO-001 : AUCUNE inspection du processus (pas de GetWindowThreadProcessId, pas
/// d'OpenProcess). L'identité vient du titre (H-01).
/// [DECISION] Format observé du client Unity : classe <c>UnityWndClass</c> et titre
/// <c>«Nom - Classe - Version - Type»</c> (ex. « Seumeurblood - Sacrieur - 3.6.12.16 - Release »).
/// Le critère est <b>structurel</b> — classe Unity + ≥ 4 segments dont l'avant-dernier est une version
/// <c>X.Y…</c> — plutôt que fondé sur une liste de classes de personnage (fragile : localisation FR/EN,
/// nouvelles classes). Le nom de personnage est le 1er segment. Isolé en fonctions pures testables
/// (couvre le risque de changement de format — spec §3.x).
/// </summary>
public static class DofusWindowRecognizer
{
    /// <summary>Classe Win32 des fenêtres du client Unity DOFUS.</summary>
    private const string UnityWindowClass = "UnityWndClass";

    /// <summary>Séparateur des segments du titre (« Nom - Classe - Version - Type »).</summary>
    private const string SegmentSeparator = " - ";

    /// <summary>Nombre minimal de segments d'un titre de client en jeu (Nom, Classe, Version, Type).</summary>
    private const int MinSegments = 4;

    /// <summary>Nombre exact de segments d'un client <b>sans personnage</b> : « Dofus &lt;version&gt; » et « Type ».</summary>
    private const int AnonymousSegments = 2;

    /// <summary>Préfixe du 1er segment d'un client sans personnage : « Dofus » suivi d'un espace, puis la version.</summary>
    private const string AnonymousNamePrefix = "Dofus ";

    /// <summary>
    /// Vrai si la fenêtre est un client DOFUS porteur d'un nom de personnage : classe
    /// <c>UnityWndClass</c> et titre conforme au format en jeu. Le launcher (autre classe) est écarté.
    /// </summary>
    public static bool IsDofusWindow(string? title, string? className)
        => string.Equals(className, UnityWindowClass, StringComparison.OrdinalIgnoreCase)
           && ExtractCharacterName(title) is not null;

    /// <summary>
    /// Vrai si la fenêtre est un client DOFUS <b>sans personnage connecté</b> (écran de sélection/connexion,
    /// Axe 11) : classe <c>UnityWndClass</c> et titre au format <c>«Dofus &lt;version&gt; - Type»</c> (ex.
    /// « Dofus 3.6.12.16 - Release »). Le critère reste <b>structurel</b> (ref [DT-010]) : exactement 2
    /// segments (le séparateur « - » n'apparaît qu'avant le type), le 1er étant « Dofus » suivi d'une version.
    /// Un client en jeu (« Nom - Classe - Version - Type », 4 segments) n'est pas concerné.
    /// </summary>
    public static bool IsAnonymousClient(string? title, string? className)
        => string.Equals(className, UnityWindowClass, StringComparison.OrdinalIgnoreCase)
           && IsAnonymousTitle(title);

    /// <summary>Fonction pure : vrai si <paramref name="title"/> a la structure « Dofus &lt;version&gt; - Type ».</summary>
    private static bool IsAnonymousTitle(string? title)
    {
        if (string.IsNullOrWhiteSpace(title)) return false;

        var segments = title.Split(SegmentSeparator);
        if (segments.Length != AnonymousSegments) return false;

        var first = segments[0].Trim(); // ex. « Dofus 3.6.12.16 »
        if (!first.StartsWith(AnonymousNamePrefix, StringComparison.OrdinalIgnoreCase)) return false;

        // Le reste après « Dofus » est la version : ancre structurelle robuste (pas de liste de types en dur).
        return LooksLikeVersion(first[AnonymousNamePrefix.Length..].Trim());
    }

    /// <summary>
    /// Extrait le nom de personnage du <paramref name="title"/> d'une fenêtre DOFUS en jeu.
    /// Fonction pure : retourne le 1er segment si le titre a la structure
    /// <c>Nom - … - Version - Type</c> (avant-dernier segment = version <c>X.Y…</c>), sinon <c>null</c>
    /// — écran de sélection, launcher ou format inconnu (RG-D01).
    /// </summary>
    public static string? ExtractCharacterName(string? title)
    {
        if (string.IsNullOrWhiteSpace(title)) return null;

        var segments = title.Split(SegmentSeparator);
        if (segments.Length < MinSegments) return null;

        // L'avant-dernier segment est la version : ancre robuste du format « en jeu ».
        if (!LooksLikeVersion(segments[^2])) return null;

        var name = segments[0].Trim();
        return name.Length == 0 ? null : name;
    }

    /// <summary>Vrai si <paramref name="value"/> ressemble à une version (chiffres et points, au moins un point).</summary>
    private static bool LooksLikeVersion(string value)
    {
        var hasDot = false;
        var hasDigit = false;
        foreach (var c in value)
        {
            if (c == '.') hasDot = true;
            else if (char.IsAsciiDigit(c)) hasDigit = true;
            else return false;
        }
        return hasDot && hasDigit;
    }
}
