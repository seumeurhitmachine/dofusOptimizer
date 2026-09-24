---
description: Phase 5 — Génère CHANGELOG.md (format Keep a Changelog) et met à jour README.md pour la version vX.Y.Z.
argument-hint: [vX.Y.Z]
allowed-tools: Read, Write, Bash(git log:*, git tag:*, git status:*)
---

Invoque le skill `livraison` pour la version **$ARGUMENTS**.

Prérequis vérifiés avant de démarrer :
- Tous les Axes de la version archivés dans `axes/`.
- `CHANGELOG.md` existant ou à créer.

Reconstruit le changelog depuis les commits (`git log [tag-précédent]..HEAD`) et les archives `axes/`. Ne crée pas le tag automatiquement — l'utilisateur décide du moment.

Procédure détaillée : voir `.claude/skills/livraison/SKILL.md`.
