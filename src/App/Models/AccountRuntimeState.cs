namespace DofusSwitcher.Models;

/// <summary>
/// Vue runtime d'un compte : résultat de la fusion « config persistée + fenêtres détectées »
/// (voir <c>AccountMerge</c>). Porte l'état <see cref="IsConnected"/>/absent et le
/// <see cref="Handle"/>, tous deux calculés au runtime et JAMAIS persistés (data-model §Conventions).
/// [ARCH] Distinct de <see cref="AccountConfig"/> : celui-ci est la ligne persistée, celle-ci l'état
/// vivant affiché. La séparation évite d'écrire l'état volatile dans le fichier de config.
/// </summary>
/// <param name="CharacterName">Nom de personnage — clé naturelle (RG-D01).</param>
/// <param name="IsConnected">Vrai si une fenêtre DOFUS correspondante existe actuellement (RG-D02).</param>
/// <param name="IsExcluded">Recopié depuis la config : compte retiré de la rotation mais conservé (EF-09).</param>
/// <param name="Handle">HWND de la fenêtre si connecté, sinon <c>0</c>. Utilisé à l'Axe 6.</param>
/// <param name="AccountName">Compte lié (recopié de la config, v2), ou <c>null</c> si non lié. Pilote les 3 zones.</param>
/// <param name="HasCharacter">
/// Faux pour un client connecté <b>sans personnage</b> (Axe 11) : affiché « Dofus N », fermable et inclus dans
/// la rotation, mais jamais persisté, sans compte ni raccourci. Vrai (défaut) pour un personnage réel.
/// </param>
public readonly record struct AccountRuntimeState(
    string CharacterName,
    bool IsConnected,
    bool IsExcluded,
    nint Handle,
    string? AccountName = null,
    bool HasCharacter = true);
