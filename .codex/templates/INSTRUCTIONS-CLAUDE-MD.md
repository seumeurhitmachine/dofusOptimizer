<!-- INSTRUCTIONS CODEX
Template de référence. NE JAMAIS modifier.
Produire : claude.md à la racine du repo (Axe en cours) ou axes/ (archivé).
Si ce fichier n'est pas dans le contexte : demander à l'utilisateur, ne rien produire.
-->

# Instructions — Génération de claude.md

**Version :** 3.0

---

## Principe

Le claude.md est un **pointeur machine**, pas un document humain.
Il ne duplique rien, n'explique rien. Il pointe et liste.

Codex dispose déjà en contexte permanent de :
- `docs/agent/project-context.md` → stack, conventions, arborescence
- `docs/agent/working-style.md` → comportement
- `docs/agent/project-state.md` §État courant → état fonctionnel

Le claude.md ajoute uniquement : quoi faire, où, avec quelles contraintes locales.

---

## Structure obligatoire

```markdown
# Axe [N] : [Titre]

refs: [chemins exacts des fichiers à lire — avec §sections si fichier long]
date: YYYY-MM-DD

## tasks
- [ ] [action] — [fichier:ligne ou §spec] — [contrainte si non-évidente]
- [ ] [action] — [fichier]

## constraints
- [contrainte locale non couverte par working-style ou project-context]
- ref: project-state.md [DT-00X] si applicable

## acceptance
Given [ctx] When [action] Then [résultat]
Given [ctx] When [action] Then [résultat]
```

---

## Règles strictes

**Taille :** < 80 lignes. Pas d'exception.

**`refs` :** fichiers à lire en plus du contexte permanent. Chemins exacts.
Sections précises si le fichier est long (`docs/specs/auth.md §2.1-2.3`).
Pendant les Axes initiaux : pointer les specs concernées.
Post-Axes initiaux : pointer les fichiers source et `docs/data-model.md` si applicable.

**`tasks` :** une ligne par tâche — action + localisation + contrainte éventuelle.
Zéro prose.

**`constraints` :** uniquement ce qui n'est pas déjà dans `working-style.md`
ou `project-context.md`. Ne pas répéter l'existant.

**`acceptance` :** Given/When/Then en une ligne chacun.
Seulement les scénarios de cet Axe.

---

## Interdit

- Section "Objectif" en prose — le titre de l'Axe suffit
- User Stories complètes — référencer par ID et §section
- Architecture globale — dans `project-context.md`
- Toute information déjà présente dans un autre fichier de contexte
- Explications de frameworks ou langages

---

## Exemple — Axe initial

```markdown
# Axe 4 : Auth JWT

refs: docs/specs/auth.md §2.1-2.3, src/auth/
date: 2026-04-21

## tasks
- [ ] JWT service — src/auth/token.service.ts — access 15min / refresh 7d
- [ ] Refresh endpoint — src/api/auth/refresh.ts
- [ ] Route guard middleware — src/middleware/auth.ts
- [ ] Tests unitaires — src/auth/token.service.test.ts

## constraints
- Pas de nouvelle dépendance sans demande explicite
- ref: project-state.md [DT-004] rotation des clés

## acceptance
Given valid credentials When POST /auth/refresh Then 200 + new access token
Given expired refresh token When POST /auth/refresh Then 401
Given missing token When GET /api/protected Then 401
```

## Exemple — Axe post-initial

```markdown
# Axe 7 : Pagination curseur

refs: docs/data-model.md, src/api/items/, src/hooks/useItems.ts
date: 2026-05-10

## tasks
- [ ] Cursor-based pagination — src/api/items/list.ts — voir [ALGO] dans fichier
- [ ] Hook useItems — src/hooks/useItems.ts — remplace pagination offset
- [ ] Mettre à jour docs/algos/pagination-cursor.md

## constraints
- Aucun findMany sans take en production — ref: project-context.md §Performance

## acceptance
Given 150 items When GET /api/items?cursor=X&limit=20 Then 20 items + nextCursor
Given last page When GET /api/items?cursor=X Then items + nextCursor: null
```

---

## Variante : corrections post-recette

```markdown
# Corrections Axe [N]

ref: axes/axe-N-[titre].md
date: YYYY-MM-DD

## critical
- BUG-001: [titre] — attendu: X / observé: Y — repro: [étapes]

## minor
- BUG-002: [titre] — [fichier:ligne]
```

Maximum 50 lignes.

---

## Checklist avant livraison

- [ ] < 80 lignes
- [ ] `refs` = chemins exacts avec sections
- [ ] Zéro prose, zéro duplication du contexte permanent
- [ ] `constraints` = local uniquement
- [ ] `acceptance` = Axe en cours seulement
