---
description: Audit d'une application existante contre l'architecture cible active, notamment Expo/Prisma.
argument-hint: [contexte ou objectif d'audit]
---

# Commande — Audit architecture

Déclenche un audit factuel, priorisé et non destructif. Ne pas modifier le code
pendant cette commande sauf demande explicite séparée.

Le rapport final doit être créé dans `./audit/RAPPORT_YYYY-MM-DD.md`, avec la
date du jour, à partir du template commun du skill audit.

Prérequis vérifiés avant de démarrer :
- Projet existant disponible.
- `docs/agent/archis/` contient l'architecture cible ou l'utilisateur la fournit.
- Pour Expo/Prisma, lire uniquement ici la référence d'audit migration du skill.

Procédure détaillée : voir `.claude/skills/audit/SKILL.md`.
