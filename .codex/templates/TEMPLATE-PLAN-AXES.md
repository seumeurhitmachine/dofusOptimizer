<!-- INSTRUCTIONS CODEX
Template de référence. NE JAMAIS modifier.
Produire : docs/plan-axes.md dans le repo du projet.

RÈGLES :
- Générer APRÈS validation de docs/specs/ et docs/agent/project-context.md.
- Chaque Axe doit tracer vers des User Stories et fichiers de specs.
- Un Axe = une session de 2-3h max. Découper si la complexité est élevée.
- L'Axe 1 n'a jamais de prérequis — c'est le scaffolding ou l'infrastructure de base.
-->

# [Nom du projet] — Plan des Axes

**Version :** 0.1
**Date :** YYYY-MM-DD
**Référence :** `docs/specs/_index.md`
**Archi active :** `docs/agent/archis/ARCHI-...md`

> Découpage en Axes de développement ordonnés.
> Chaque Axe = un lot fonctionnel cohérent + une branche Git + un claude.md.
> Feuille de route pour la Phase 3 (Développement).

---

## Vue d'ensemble

| Axe | Titre | US couvertes | Priorité | Complexité | Dépendances |
|---|---|---|---|---|---|
| 1 | [Titre] | US-001, US-002 | Must | faible | — |
| 2 | [Titre] | US-003, US-004 | Must | moyenne | Axe 1 |
| 3 | [Titre] | US-005 | Should | faible | Axe 1 |

---

## Axe 1 — [Titre]

**Périmètre :**
- US-001 — [Titre] (`docs/specs/[domaine].md §2.4`)
- US-002 — [Titre] (`docs/specs/[domaine].md §2.4`)
- RG-001, RG-002 (`docs/specs/[domaine].md §2.5`)

**Specs concernées :** `docs/specs/[domaine].md §X.Y–Z.W`
**Archis concernées :** `docs/agent/archis/[archi].md §...`
**ENF associées :** ENF-001 (si applicable)
**Prérequis :** aucun
**Complexité estimée :** faible | moyenne | élevée

---

## Axe 2 — [Titre]

**Périmètre :**
- US-003 — [Titre] (`docs/specs/[domaine].md §2.4`)
- US-004 — [Titre] (`docs/specs/[domaine].md §2.4`)

**Specs concernées :** `docs/specs/[domaine].md §X.Y–Z.W`
**Archis concernées :** `docs/agent/archis/[archi].md §...`
**ENF associées :** —
**Prérequis :** Axe 1 mergé
**Complexité estimée :** faible | moyenne | élevée

---

## Matrice de couverture

> Toutes les US Must/Should doivent être affectées à un Axe.

| US | Titre | Priorité | Axe | Couvert |
|---|---|---|---|---|
| US-001 | [Titre] | Must | 1 | ✓ |
| US-002 | [Titre] | Must | 1 | ✓ |
| US-003 | [Titre] | Must | 2 | ✓ |
| US-004 | [Titre] | Should | 2 | ✓ |
| US-005 | [Titre] | Could | — | Reporté |

---

## Notes

- Axes Must en priorité, dans l'ordre de dépendance.
- Axes Should/Could réordonnables selon le feedback de recette.
- Ce document est mis à jour si le périmètre d'un Axe change.
- Chaque Axe qui touche la stack doit référencer les sections d'archi concernées.
  Pour Expo/Prisma : repo, backend, auth, prisma, frontend, docker ou CI/CD selon
  les fichiers touchés.
- Après les Axes initiaux, les nouveaux Axes ne référencent plus les specs —
  ils pointent vers les fichiers source, `docs/data-model.md` et les archis actives.
