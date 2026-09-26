namespace DofusSwitcher.Models;

/// <summary>
/// Une fenêtre de client DOFUS observée au runtime : nom de personnage extrait du titre + handle natif.
/// [ARCH] Purement runtime — JAMAIS persisté (l'état connecté/absent et le <see cref="Handle"/> se
/// recalculent à chaque exécution, data-model §Conventions « Runtime vs persisté »). Distinct de
/// <see cref="AccountConfig"/> (config persistée). Émis par <c>IWindowDetector</c>.
/// </summary>
/// <param name="CharacterName">
/// Nom de personnage — clé naturelle de rapprochement avec la config (RG-D01). Pour un client <b>sans
/// personnage</b> (<see cref="HasCharacter"/> = <c>false</c>, Axe 11), porte une clé synthétique dérivée du
/// handle (stable et sans collision), jamais un vrai nom : elle sert uniquement d'identité runtime.
/// </param>
/// <param name="Handle">HWND de la fenêtre, réutilisé à l'Axe 6 pour la bascule de focus.</param>
/// <param name="HasCharacter">
/// Faux pour un client DOFUS connecté <b>sans personnage</b> (écran de sélection « Dofus - Version - Release »,
/// Axe 11) : participant runtime de la rotation mais jamais persisté, sans compte ni raccourci. Vrai (défaut)
/// pour un client en jeu porteur d'un nom de personnage.
/// </param>
public readonly record struct DetectedWindow(string CharacterName, nint Handle, bool HasCharacter = true);
