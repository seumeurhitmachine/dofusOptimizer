---
description: Démarre le développement d'un Axe. Lit le contexte projet et propose un plan avant de coder.
argument-hint: [numéro d'axe]
allowed-tools: Read, Write, Bash(git *)
---

Démarrage de l'Axe $ARGUMENTS.

## Étape 1 — Lecture contexte permanent

Lire dans cet ordre, sans exception :
1. `docs/agent/project-context.md`
2. `docs/agent/working-style.md`
3. Section "État courant" de `docs/agent/project-state.md` uniquement
4. `claude.md` — vérifier qu'il correspond à l'Axe $ARGUMENTS.
   Si non : signaler et arrêter.
5. `docs/ui-design.md` si présent et si l'Axe touche à l'UI.
6. Fichiers listés dans la section `refs` de `claude.md`.

## Étape 2 — Branche Git

Vérifier la branche courante : `git branch --show-current`
Si elle n'est pas `axe-$ARGUMENTS-*` :
`git checkout -b axe-$ARGUMENTS-[titre-court-depuis-claude.md]`

## Étape 3 — Plan

Proposer un plan d'implémentation structuré (fichiers à créer, à modifier,
ordre logique) avant d'écrire la moindre ligne de code.
Attendre validation explicite avant de commencer.

Ne pas poser de questions sur les détails d'implémentation — décider,
ajouter un commentaire [DECISION] dans le code, continuer.
Poser une question uniquement si l'ambiguïté bloque une décision irréversible.
