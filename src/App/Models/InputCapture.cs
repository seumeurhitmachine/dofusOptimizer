namespace DofusSwitcher.Models;

/// <summary>Bouton souris capté par la modale, indépendant de tout type WPF.</summary>
public enum CapturedMouseButton
{
    Left,
    Right,
    Middle,
    XButton1,
    XButton2,
}

/// <summary>
/// Cœur <b>pur</b> de la capture d'entrée : encodage d'un bouton/touche en <see cref="Binding"/>,
/// règles de rejet (combinaison/modificateur seul, D-01) et formatage lisible.
/// [ARCH] Aucun type WPF ici (testable seul). Le code-behind de la modale traduit les événements
/// WPF (<c>PreviewKeyDown</c>/<c>PreviewMouseDown</c>) en appels à ces helpers.
/// </summary>
public static class InputCapture
{
    // [DECISION] Encodage des boutons souris dans Binding.Code (data-model §Binding). XButton1/2
    // conservent 1/2 pour rester cohérents avec les défauts AppConstants.DefaultPrev/NextButtonCode ;
    // les boutons principaux prennent des codes distincts au-delà. Toute la souris est acceptée
    // (y compris auxiliaires) — pas de whitelist (mémoire projet : bindings-accept-any-button).
    private const int CodeXButton1 = 1;
    private const int CodeXButton2 = 2;
    private const int CodeLeft = 3;
    private const int CodeRight = 4;
    private const int CodeMiddle = 5;

    // Virtual-Key codes des modificateurs (génériques + variantes gauche/droite) et touches Windows.
    private static readonly HashSet<int> ModifierVirtualKeys =
    [
        0x10, 0x11, 0x12,             // Shift, Control, Menu (Alt) génériques
        0xA0, 0xA1, 0xA2, 0xA3,       // L/R Shift, L/R Control
        0xA4, 0xA5,                   // L/R Menu (Alt)
        0x5B, 0x5C,                   // L/R Windows
    ];

    /// <summary>Encode un bouton souris capté en <see cref="Binding"/> (kind <see cref="BindingKind.MouseButton"/>).</summary>
    public static Binding ToBinding(CapturedMouseButton button) => new(BindingKind.MouseButton, button switch
    {
        CapturedMouseButton.Left => CodeLeft,
        CapturedMouseButton.Right => CodeRight,
        CapturedMouseButton.Middle => CodeMiddle,
        CapturedMouseButton.XButton1 => CodeXButton1,
        CapturedMouseButton.XButton2 => CodeXButton2,
        _ => CodeLeft,
    });

    /// <summary>Encode une touche clavier (Virtual-Key code) en <see cref="Binding"/>.</summary>
    public static Binding FromKey(int virtualKey) => new(BindingKind.Key, virtualKey);

    /// <summary>Vrai si le Virtual-Key est un modificateur seul (Shift/Ctrl/Alt/Win) — jamais assignable (D-01).</summary>
    public static bool IsModifierVirtualKey(int virtualKey) => ModifierVirtualKeys.Contains(virtualKey);

    /// <summary>
    /// Vrai si une touche doit être rejetée : soit un modificateur pris seul, soit une combinaison
    /// (une touche pressée alors qu'un modificateur est maintenu). Une seule entrée nue est acceptée.
    /// </summary>
    public static bool ShouldRejectKey(int virtualKey, bool modifiersHeld)
        => modifiersHeld || IsModifierVirtualKey(virtualKey);

    /// <summary>
    /// Rendu lisible d'un <see cref="Binding"/> pour l'UI (« XButton2 », « F1 », « A », « Espace »).
    /// Fallback explicite pour les touches non nommées, pour ne jamais afficher un code brut ambigu.
    /// </summary>
    public static string Format(Binding binding) => binding.Kind switch
    {
        BindingKind.MouseButton => FormatMouse(binding.Code),
        BindingKind.Key => FormatKey(binding.Code),
        _ => $"?{binding.Code}",
    };

    private static string FormatMouse(int code) => code switch
    {
        CodeXButton1 => "XButton1",
        CodeXButton2 => "XButton2",
        CodeLeft => "Clic gauche",
        CodeRight => "Clic droit",
        CodeMiddle => "Clic milieu",
        _ => $"Souris {code}",
    };

    private static string FormatKey(int vk) => vk switch
    {
        >= 0x41 and <= 0x5A => ((char)vk).ToString(),        // A–Z
        >= 0x30 and <= 0x39 => ((char)vk).ToString(),        // 0–9
        >= 0x70 and <= 0x87 => $"F{vk - 0x6F}",              // F1–F24
        0x20 => "Espace",
        0x0D => "Entrée",
        0x09 => "Tab",
        0x08 => "Retour",
        0x1B => "Échap",
        0x2D => "Inser",
        0x2E => "Suppr",
        0x24 => "Origine",
        0x23 => "Fin",
        0x21 => "PgPréc",
        0x22 => "PgSuiv",
        0x25 => "←",
        0x26 => "↑",
        0x27 => "→",
        0x28 => "↓",
        _ => $"Touche {vk}",
    };
}
