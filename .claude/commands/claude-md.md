---
description: Phase 2.6 / cycle — Génère le claude.md de l'Axe N (format machine-readable < 80 lignes).
argument-hint: [numéro d'axe]
allowed-tools: Read, Write
---

Invoque le skill `claude-md` pour l'Axe **$ARGUMENTS**.

Prérequis vérifiés avant de démarrer :
- `docs/plan-axes.md` validé.
- `.claude/templates/INSTRUCTIONS-CLAUDE-MD.md` disponible.
- `docs/agent/project-state.md` disponible (pour déterminer la phase du projet : Axes initiaux ou évolution).

Règle : le fichier produit remplace `claude.md` à la racine. L'ancien doit déjà être archivé via `/finalise` (sinon signaler et arrêter).

Procédure détaillée : voir `.claude/skills/claude-md/SKILL.md`.
