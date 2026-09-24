---
description: Phase 2.1 — Génère docs/specs/[domaine].md par domaine + docs/specs/_index.md à partir du template 00-SPEC-CADRAGE-TEMPLATE.md.
argument-hint: [profil sécurité — minimal | standard | renforcé]
allowed-tools: Read, Write, Bash(git status:*, git diff:*)
---

Invoque le skill `cadrage` avec le profil sécurité **$ARGUMENTS**.

Prérequis vérifiés avant de démarrer :
- Note d'intention validée et questions de clarification répondues.
- `.claude/templates/00-SPEC-CADRAGE-TEMPLATE.md` disponible.

Si le profil n'est pas fourni ou est inconnu : demander à l'utilisateur avant de produire quoi que ce soit (profils valides : `minimal` | `standard` | `renforcé`).

Procédure détaillée : voir `.claude/skills/cadrage/SKILL.md`.
