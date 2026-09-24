namespace DofusSwitcher.Models;

/// <summary>
/// Une fenêtre de client DOFUS observée au runtime : nom de personnage extrait du titre + handle natif.
/// [ARCH] Purement runtime — JAMAIS persisté (l'état connecté/absent et le <see cref="Handle"/> se
/// recalculent à chaque exécution, data-model §Conventions « Runtime vs persisté »). Distinct de
/// <see cref="AccountConfig"/> (config persistée). Émis par <c>IWindowDetector</c>.
/// </summary>
/// <param name="CharacterName">Nom de personnage — clé naturelle de rapprochement avec la config (RG-D01).</param>
/// <param name="Handle">HWND de la fenêtre, réutilisé à l'Axe 6 pour la bascule de focus.</param>
public readonly record struct DetectedWindow(string CharacterName, nint Handle);
