namespace DofusSwitcher.Models;

/// <summary>
/// Configuration persistée d'un <b>personnage</b> DOFUS. Conservé même quand son client est absent :
/// la présence en config est indépendante de l'existence d'une fenêtre (état connecté/absent
/// et HWND calculés au runtime, jamais persistés — data-model §AccountConfig).
/// [DECISION] Nom de type <c>AccountConfig</c> conservé (historique v1) bien qu'il modélise un
/// <b>personnage</b> : le renommer casserait la clé JSON <c>accounts</c> (compat) et rippserait sur
/// tout le code/les tests. L'entité <b>compte</b> introduite en v2 est <see cref="GameAccount"/> ;
/// le lien personnage→compte est porté par <see cref="AccountName"/>.
/// </summary>
/// <param name="CharacterName">
/// Nom du personnage — clé naturelle non vide et unique dans <c>accounts</c> (identité stable,
/// cohérente avec la détection par titre et la conservation hors ligne).
/// </param>
/// <param name="Excluded">Retiré de la rotation mais conservé et réactivable (EF-09).</param>
/// <param name="DirectBinding">Entrée d'activation directe, optionnelle ; soumise à l'unicité globale.</param>
/// <param name="AccountName">
/// Nom du <see cref="GameAccount"/> auquel ce personnage est lié (RG-C02), ou <c>null</c> si non lié.
/// Absent des configs v1 → <c>null</c> à la migration (personnage non lié). Lien établi manuellement (RG-C06).
/// </param>
public sealed record AccountConfig(
    string CharacterName,
    bool Excluded = false,
    Binding? DirectBinding = null,
    string? AccountName = null);
