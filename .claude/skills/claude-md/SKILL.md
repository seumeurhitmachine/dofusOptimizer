---
name: claude-md
description: Génère le claude.md pour un Axe donné. Format machine-readable < 80 lignes. Invoquer avec le numéro d'Axe en argument.
argument-hint: [numéro d'axe]
---

# Skill — Génération claude.md

## Prérequis

- `docs/plan-axes.md` validé.
- `.claude/templates/INSTRUCTIONS-CLAUDE-MD.md` disponible.
- `docs/agent/project-state.md` disponible (pour identifier la phase du projet).
- Numéro d'Axe fourni en argument.

## Procédure

1. Lire `.claude/templates/INSTRUCTIONS-CLAUDE-MD.md` intégralement.
2. Lire la définition de l'Axe $ARGUMENTS dans `docs/plan-axes.md`.
3. Identifier la phase du projet depuis `project-state.md` §État courant :
   - **Axes initiaux en cours** → `refs` pointe vers `docs/specs/[domaine].md §sections`
     et les sections d'archi listées dans l'Axe.
   - **Post-Axes initiaux** → `refs` pointe vers les fichiers source et
     `docs/data-model.md` si l'Axe touche au modèle, plus les archis actives.
     Pas de ref aux specs.
4. Construire la section `tasks` :
   - Une ligne par tâche — action + localisation + contrainte si non-évidente.
   - Référencer les specs par `§section` et ID, pas les recopier.
5. Construire la section `constraints` :
   - Uniquement ce qui n'est pas dans `working-style.md` ou `project-context.md`.
6. Construire la section `acceptance` :
   - Given/When/Then en une ligne chacun.
   - Seulement les scénarios de cet Axe.
7. Vérifier le plafond : < 80 lignes. Si dépassé : synthétiser les tasks,
   pas les acceptance.
8. Produire `claude.md` à la racine. Signaler à l'utilisateur.

## Règles issues de INSTRUCTIONS-CLAUDE-MD.md

- Zéro prose — format pointeur uniquement.
- Jamais de section "Objectif" — le titre de l'Axe suffit.
- Jamais d'info déjà présente dans le contexte permanent.
- Post-Axes initiaux : ne pas référencer les specs, elles sont archivées.
- Ne pas référencer le document d'audit migration Expo/Prisma dans `claude.md`
  sauf si l'Axe est explicitement un audit/migration déclenché par le skill `audit`.
