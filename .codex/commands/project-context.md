---
description: Phase 2.3 — Génère les trois fichiers docs/agent/ (project-context.md, working-style.md, project-state.md) en instanciant l'archi détectée.
allowed-tools: Read, Write
---

Invoque le skill `project-context`.

Prérequis vérifiés avant de démarrer :
- `docs/specs/` validé (au minimum `docs/specs/_index.md`).
- Templates `TEMPLATE-PROJECT-CONTEXT.md`, `TEMPLATE-WORKING-STYLE.md`, `TEMPLATE-PROJECT-STATE.md` disponibles.
- `docs/agent/archis/` peut contenir zéro ou plusieurs fichiers d'archi.

Résolution de l'archi : front matter YAML `stack:` de chaque fichier `docs/agent/archis/*.md`. Si ambigu, demander.

Procédure détaillée : voir `.codex/skills/project-context/SKILL.md`.
