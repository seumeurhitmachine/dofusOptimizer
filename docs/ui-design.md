# Dofus Window Switcher — UI Design Reference

**Version :** 1.0
**Date :** 2026-09-24
**Ref specs :** `docs/specs/` (stack §3.2) · **Archi :** `docs/agent/archis/ARCHI-DOTNET-WPF.md`

> Application desktop Windows (WPF). Une fenêtre de configuration à onglets + un
> menu de zone de notification. Pas de mobile, pas de web, pas d'authentification.

---

## Ton visuel

Épuré, technique, discret — **thème sombre unique** — Ref : Paramètres Windows 11
(sobriété), outils gaming en tray. Un seul thème à maintenir.

## Palette

> Centralisée dans un `ResourceDictionary` XAML (`Themes/Colors.xaml`) — jamais de
> HEX en dur dans les vues ou le code-behind (convention archi §Règles de constantes ;
> équivalent WPF du `constants/colors.ts` du template générique).

| Token | HEX | Usage |
|---|---|---|
| primary | #3AAFA9 | Accent : élément actif, CTA, focus, sélection |
| primaryHover | #2E8B87 | Survol/pressé de l'accent |
| secondary | #5B9BD5 | Accents secondaires, info neutre |
| background | #1E1F22 | Fond principal de la fenêtre |
| surface | #2B2D31 | Panneaux, cartes, champs, lignes de liste |
| surfaceAlt | #35373C | Survol de ligne, en-têtes, séparateurs |
| textPrimary | #E4E6EB | Texte principal |
| textSecondary | #9AA0A6 | Labels, placeholders, états secondaires |
| success | #4CAF6E | Compte connecté, validation |
| error | #E5534B | Conflit d'association, champ invalide |
| warning | #E0A33E | Compte exclu, avertissement |
| info | #5B9BD5 | Informations neutres |

## Typographie

Police **système Segoe UI** (in-box Windows) — titres et corps identiques, sobre.

| Rôle | Police | Taille | Poids |
|---|---|---|---|
| Titre fenêtre / onglet | Segoe UI | 14 px | SemiBold |
| Section (H2) | Segoe UI | 13 px | SemiBold |
| Corps | Segoe UI | 13 px | Regular |
| Label | Segoe UI | 12 px | Regular |
| Caption / état | Segoe UI | 11 px | Regular |

## Navigation

Pattern : **onglets** (`TabControl`) dans une fenêtre unique redimensionnable
(taille par défaut ~480×420, min ~420×360). Pas d'authentification.

Entrées principales :
1. **Comptes** — liste ordonnée, état temps réel, réordonnancement, exclusion.
2. **Raccourcis** — associations « suivant / précédent » + directes par compte,
   signalement de conflit.
3. **Réglages** — suspendre l'interception, démarrage Windows (option off par
   défaut), import/export de la configuration, à-propos.

Modales récurrentes :
- **Capture d'entrée** — « Appuyez sur une touche ou un bouton… » (assignation
  d'une association ; Échap = annuler). Voir `docs/wireframes/capture-modal.html`.

Zone de notification (tray) :
- Double-clic icône → affiche/masque la fenêtre.
- Menu : Ouvrir · Suspendre/Réactiver l'interception (case) · Quitter.
- L'icône reflète l'état suspendu (variante grisée / badge).

## Composants clés

| Composant | Notes |
|---|---|
| Liste des comptes | `ItemsControl`/`ListBox`, courte (nb de clients), pas de pagination. Réordonnancement **boutons ↑/↓ + glisser-déposer**. Poignée `☰` visible. |
| Ligne de compte | Nom de personnage + pastille d'état (● vert connecté / ○ gris absent) + badge « exclu » (warning). |
| Assignation d'entrée | Bouton affichant l'entrée courante (« XButton2 ») ; clic → modale de capture. Bouton « Effacer ». |
| Indicateur de conflit | Icône + libellé error inline sous l'association en conflit ; empêche l'enregistrement. |
| Interrupteurs | `ToggleSwitch`/`CheckBox` (suspendre, démarrage Windows). |
| Notifications | In-app discrètes (bandeau) + info-bulle tray optionnelle. Pas de push. |

## Interactions clés

- **Réordonner** : sélectionner une ligne → `↑`/`↓` ; ou glisser la poignée `☰`.
  L'ordre se répercute immédiatement et déclenche la sauvegarde (débouncée).
- **Assigner** : clic sur le bouton d'entrée → modale « appuyez sur une entrée » →
  capture d'une touche/bouton unique (combinaisons rejetées, D-01) → si conflit,
  message error et refus ; sinon enregistrement.
- **Exclure** : bouton/toggle sur la ligne → badge « exclu », retrait de la rotation.
- **Fermer la fenêtre** : la masque (l'app reste en tray) — voir `tray.md`.

## Contraintes

- Charte : aucune imposée ; identité sobre définie ici.
- Cibles : **Windows 10/11 x64 desktop uniquement**.
- Accessibilité : contraste texte/fond ≥ WCAG AA sur le thème sombre ; navigation
  clavier complète (Tab/flèches) ; l'état n'est jamais transmis par la seule
  couleur (pastille + libellé texte).

## Wireframes

Voir `docs/wireframes/` — un fichier HTML basse fidélité par écran clé :
`comptes.html`, `raccourcis.html`, `reglages.html`, `capture-modal.html`,
`tray-menu.html`.
