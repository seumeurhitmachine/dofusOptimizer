---
description: Phase 2.4 / actif — Génère docs/data-model.md initial (mode init) ou met à jour un algo dans docs/algos/ (mode algo).
argument-hint: [init | algo nom-algo]
allowed-tools: Read, Write
---

Invoque le skill `data-model` avec l'argument **$ARGUMENTS**.

Modes :
- `init` — génération initiale de `docs/data-model.md` (Phase 2.4, une seule fois).
- `algo <nom>` — production ou mise à jour de `docs/algos/<nom>.md`. À faire dans la même session qu'une modification d'algo, pas en fin d'Axe.

Procédure détaillée : voir `.claude/skills/data-model/SKILL.md`.
