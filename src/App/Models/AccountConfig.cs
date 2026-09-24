namespace DofusSwitcher.Models;

/// <summary>
/// Configuration persistée d'un compte DOFUS. Conservé même quand son client est absent :
/// la présence en config est indépendante de l'existence d'une fenêtre (état connecté/absent
/// et HWND calculés au runtime, jamais persistés — data-model §AccountConfig).
/// </summary>
/// <param name="CharacterName">
/// Nom du personnage — clé naturelle non vide et unique dans <c>accounts</c> (identité stable,
/// cohérente avec la détection par titre et la conservation hors ligne).
/// </param>
/// <param name="Excluded">Retiré de la rotation mais conservé et réactivable (EF-09).</param>
/// <param name="DirectBinding">Entrée d'activation directe, optionnelle ; soumise à l'unicité globale.</param>
public sealed record AccountConfig(
    string CharacterName,
    bool Excluded = false,
    Binding? DirectBinding = null);
