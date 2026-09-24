---
description: Phase 6 — Prépare le projet pour une évolution post-version initiale (archivage specs, consolidation project-state.md, premier claude.md d'évolution). À invoquer UNE SEULE FOIS au passage en phase 6.
argument-hint: [description courte de l'évolution]
allowed-tools: Read, Write, Bash(git tag:*, git log:*)
---

Invoque le skill `evolution` avec la description **$ARGUMENTS**.

Prérequis vérifiés avant de démarrer :
- Tag de version posé (au moins `v1.0.0` ou équivalent).
- `axes/` contient les archives de tous les Axes initiaux.
- `CHANGELOG.md` à jour.

Après invocation, cycle normal : `/claude-md N` → `/axe N` → `/bugs` si besoin → `/finalise`. Les nouveaux claude.md ne référencent plus les specs — elles sont archivées.

Procédure détaillée : voir `.codex/skills/evolution/SKILL.md`.
