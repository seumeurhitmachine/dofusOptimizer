---
name: data-model
description: Génère docs/data-model.md initial ou met à jour un algo dans docs/algos/. Invoquer après validation des specs ou lors d'une modification de schéma/algo.
argument-hint: [init | algo nom-algo]
---

# Skill — Data Model & Algos

## Mode `init` — Génération initiale

### Prérequis
- `docs/specs/` validé — en particulier la spec du domaine data.
- `.codex/templates/TEMPLATE-DATA-MODEL.md` disponible dans le contexte.

### Procédure
1. Lire `.codex/templates/TEMPLATE-DATA-MODEL.md`.
2. Lire les specs concernées par le modèle de données (`docs/specs/_index.md` d'abord, puis les domaines `data`, `auth`, etc.).
3. Lire les fichiers d'archi de persistance référencés par les specs. Pour
   Expo/Prisma, lire `docs/agent/archis/ARCHI-EXPO-PRISMA-PRISMA.md` avant de produire.
4. Produire `docs/data-model.md` :
   - Diagramme ERD Mermaid complet.
   - Invariants par entité — règles métier non déductibles du schéma seul.
   - Conventions du projet (IDs, timestamps, soft delete, FK).
5. Signaler à l'utilisateur. Ne pas enchaîner sans validation.

---

## Mode `algo [nom]` — Documentation d'un algo

### Prérequis
- Fichier source de l'algo existant ou en cours de création.
- `.codex/templates/TEMPLATE-ALGO.md` disponible dans le contexte.

### Procédure
1. Lire `.codex/templates/TEMPLATE-ALGO.md`.
2. Lire le fichier source de l'algo pour comprendre l'implémentation.
3. Produire `docs/algos/[nom-algo].md` :
   - Problème résolu, contraintes, approche, complexité, cas limites.
   - Ne pas décrire le "quoi" — le code est la référence.
4. Vérifier que le fichier source contient un commentaire `[ALGO]` pointant
   vers `docs/algos/[nom-algo].md`. L'ajouter si absent.
5. Signaler à l'utilisateur.

---

## Règles communes

- Toujours mettre à jour `docs/data-model.md §Historique` après toute modification.
- Ne pas attendre `/finalise` pour mettre à jour ces fichiers — le faire dans la session.
- Ces fichiers sont actifs sur toute la durée du projet, contrairement aux specs.
- Toute évolution de schéma doit respecter l'archi active : singleton Prisma,
  migration versionnée, transaction si plusieurs écritures, pagination serveur
  pour les listes volumineuses.
