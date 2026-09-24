# Dofus Window Switcher — Plan des Axes

**Version :** 0.1
**Date :** 2026-09-24
**Référence :** `docs/specs/_index.md`
**Archi active :** `docs/agent/archis/ARCHI-DOTNET-WPF.md`

> Découpage en Axes ordonnés. Chaque Axe = un lot cohérent + une branche Git +
> un `claude.md`. Feuille de route pour la Phase 3 (Développement).

---

## Vue d'ensemble

| Axe | Titre | US couvertes | Priorité | Complexité | Dépendances |
|---|---|---|---|---|---|
| 1 | Scaffolding & socle UI | — (infra, ENF-006) | Must | moyenne | — |
| 2 | Persistance & modèle de config | US-P01, P02, P03 | Must | moyenne | Axe 1 |
| 3 | Détection des fenêtres & comptes | US-D01, D02 | Must | moy.-élevée | Axe 1, 2 |
| 4 | Ordre & exclusion (onglet Comptes) | US-D03, D04 | Must/Could | faible-moy. | Axe 2, 3 |
| 5 | Capture d'entrées & associations | US-S04, S05 | Must/Should | moyenne | Axe 1, 2 |
| 6 | Interception & bascule de focus (cœur) | US-S01, S02, S03 | Must/Should | **élevée** | Axe 3, 5 |
| 7 | Tray, cycle de vie & réglages | US-T01..T04, P04 | Should/Could | moyenne | Axe 6 |

---

## Axe 1 — Scaffolding & socle UI

**Périmètre :**
- Solution `.sln` + projet WPF `src/App` + `tests/App.Tests` (xUnit).
- `Directory.Build.props` : TFM `net10.0-windows`, publication single-file self-contained, `UseWPF`+`UseWindowsForms`, **zéro `PackageReference`** dans l'app.
- Socle MVVM : `ObservableObject`, `RelayCommand` ; composition root `App.xaml.cs` (`ShutdownMode.OnExplicitShutdown`).
- Fenêtre principale à onglets (shell vide : Comptes / Raccourcis / Réglages).
- Thème sombre : `Themes/Colors.xaml` (palette `ui-design.md`) ; `Interop/NativeMethods.cs` (squelette) ; `Resources/app.ico`.

**Specs concernées :** `docs/specs/overview.md §3.2`, `docs/ui-design.md`
**Archis concernées :** `ARCHI-DOTNET-WPF.md §Structure, §MVVM manuel, §Composition root, §Publication`
**ENF associées :** ENF-006 (couches), ENF-001 (cible Windows)
**Prérequis :** aucun · **Complexité :** moyenne

---

## Axe 2 — Persistance & modèle de config

**Périmètre :**
- Modèles `AppConfig`, `AccountConfig`, `Binding` (`docs/data-model.md`).
- `IConfigStore` + `JsonConfigStore` : source-gen `AppJsonContext`, écriture **atomique**, autosave **débouncé**, reprise sur corruption (backup + défaut), rechargement au démarrage.
- US-P01 — save/reload (`docs/specs/persistence.md §2.4`)
- US-P02 — conservation des comptes hors ligne (`persistence.md §2.4`)
- US-P03 — résilience config corrompue (`persistence.md §2.4`)
- RG-P01..P07 (`persistence.md §2.5`)

**Specs concernées :** `docs/specs/persistence.md`
**Archis concernées :** `ARCHI-DOTNET-WPF.md §Persistance, §Threading`
**ENF associées :** — · **Prérequis :** Axe 1 · **Complexité :** moyenne

---

## Axe 3 — Détection des fenêtres & comptes

**Périmètre :**
- `IWindowDetector` : `EnumWindows` + `SetWinEventHook` (create/destroy/namechange), reconnaissance via titre/classe `user32` (C-02), extraction du nom de personnage (fonction pure testable).
- Marshaling des callbacks vers le `Dispatcher`. Fusion comptes **détectés + persistés** (état `connecté/absent` runtime).
- Onglet Comptes : liste temps réel avec état (lecture).
- US-D01, US-D02 (`docs/specs/detection.md §2.4`) ; RG-D01, D02, D03, D05 (`§2.5`)

**Specs concernées :** `docs/specs/detection.md`
**Archis concernées :** `ARCHI-DOTNET-WPF.md §Interop, §Threading`
**ENF associées :** ENF-003 (détection événementielle) · **Prérequis :** Axe 1, 2 · **Complexité :** moyenne-élevée

---

## Axe 4 — Ordre & exclusion (onglet Comptes)

**Périmètre :**
- Réordonnancement boutons ↑/↓ **et** glisser-déposer ; exclusion/réintégration.
- Répercussion immédiate + sauvegarde (via Axe 2).
- US-D03 — réordonner (`docs/specs/detection.md §2.4`)
- US-D04 — exclusion temporaire (`detection.md §2.4`) ; RG-D04 (`§2.5`)

**Specs concernées :** `docs/specs/detection.md`, `docs/ui-design.md`
**Archis concernées :** `ARCHI-DOTNET-WPF.md §MVVM manuel`
**ENF associées :** — · **Prérequis :** Axe 2, 3 · **Complexité :** faible-moyenne

---

## Axe 5 — Capture d'entrées & associations

**Périmètre :**
- Modale de capture (« appuyez sur une entrée » ; rejet des combinaisons, D-01).
- Onglet Raccourcis : associations suivant/précédent + directes par compte ; défauts XButton2/XButton1.
- Détection de conflit (unicité globale des `Binding`, refus à l'enregistrement).
- US-S04 — configuration par capture (`docs/specs/switching.md §2.4`)
- US-S05 — signalement de conflit (`switching.md §2.4`) ; RG-S04, S05 (`§2.5`)

**Specs concernées :** `docs/specs/switching.md`, `docs/data-model.md §Invariants`
**Archis concernées :** `ARCHI-DOTNET-WPF.md §Interop, §MVVM manuel`
**ENF associées :** — · **Prérequis :** Axe 1, 2 · **Complexité :** moyenne

---

## Axe 6 — Interception & bascule de focus (cœur)

**Périmètre :**
- Hooks bas niveau `WH_MOUSE_LL` + `WH_KEYBOARD_LL` ; interception **conditionnée au focus DOFUS** (sinon `CallNextHookEx`).
- Rotation cyclique (saut absents/exclus) → `docs/algos/rotation.md` (`[ALGO]`).
- Activation `SetForegroundWindow` (+ `AttachThreadInput` si refus) — **non synthétique** (C-03) ; jamais de handle process (C-02).
- Callback minimal (latence < 100 ms). Activation directe.
- US-S01 (cyclique), US-S02 (directe), US-S03 (interception conditionnelle) (`docs/specs/switching.md §2.4`) ; RG-S01, S02, S03, S06 (`§2.5`) ; C-01

**Specs concernées :** `docs/specs/switching.md §2.4, §2.5, §3.x`
**Archis concernées :** `ARCHI-DOTNET-WPF.md §Interop, §Threading`
**ENF associées :** ENF-002 (latence), ENF-005 (sans élévation) · **Prérequis :** Axe 3, 5 · **Complexité :** élevée

---

## Axe 7 — Tray, cycle de vie & réglages

**Périmètre :**
- `NotifyIcon` + menu ; fermer = masquer ; `Dispose` à la sortie.
- Suspendre/réactiver l'interception (persistant, reflété dans l'icône).
- Lancement manuel + app listée dans les applications Windows (raccourci menu Démarrer) ; option démarrage Windows (off par défaut, `HKCU\Run`).
- Export/import de la configuration.
- US-T01..T04 (`docs/specs/tray.md §2.4`), US-P04 (`persistence.md §2.4`) ; RG-T01..T06

**Specs concernées :** `docs/specs/tray.md`, `docs/specs/persistence.md`
**Archis concernées :** `ARCHI-DOTNET-WPF.md §Tray, §Composition root, §Publication`
**ENF associées :** ENF-004 (tray), ENF-005 · **Prérequis :** Axe 6 · **Complexité :** moyenne

---

## Matrice de couverture

| US | Titre | Priorité | Axe | Couvert |
|---|---|---|---|---|
| US-D01 | Détecter les clients DOFUS | Must | 3 | ✓ |
| US-D02 | État des comptes temps réel | Must | 3 | ✓ |
| US-D03 | Définir/réordonner la rotation | Must | 4 | ✓ |
| US-D04 | Exclure temporairement un compte | Could | 4 | ✓ |
| US-S01 | Bascule cyclique suivant/précédent | Must | 6 | ✓ |
| US-S02 | Activation directe d'un compte | Should | 6 | ✓ |
| US-S03 | Interception conditionnée au focus | Must | 6 | ✓ |
| US-S04 | Configurer les associations par capture | Must | 5 | ✓ |
| US-S05 | Signaler les conflits | Should | 5 | ✓ |
| US-P01 | Sauvegarder et recharger | Must | 2 | ✓ |
| US-P02 | Conserver les comptes hors ligne | Must | 2 | ✓ |
| US-P03 | Résister à une config corrompue | Should | 2 | ✓ |
| US-P04 | Export / import | Could | 7 | ✓ |
| US-T01 | Vivre dans le tray | Should | 7 | ✓ |
| US-T02 | Suspendre / réactiver l'interception | Should | 7 | ✓ |
| US-T03 | Lancement manuel, listé dans Windows | Should | 7 | ✓ |
| US-T04 | Démarrage Windows (option) | Could | 7 | ✓ |

Toutes les US Must et Should sont affectées. Aucune US reportée.

---

## Notes

- **Axe 6 = axe à risque** (hooks bas niveau, focus, latence, C-02/C-03) : prévoir
  une session dédiée ; la rotation part en `docs/algos/rotation.md`.
- Axes Must (1→6) en priorité ; les Could (US-D04, P04, T04) sont livrés dans leur
  Axe hôte mais dégradables si le temps manque.
- Critères d'acceptation : CA-01/03 (Axe 6), CA-02 (Axe 6), CA-04 (Axe 2).
- Packaging « listé dans les applications Windows » (US-T03) : raccourci menu
  Démarrer à cadrer en Axe 7 (l'exe reste autonome single-file).
- Document mis à jour si le périmètre d'un Axe change.
