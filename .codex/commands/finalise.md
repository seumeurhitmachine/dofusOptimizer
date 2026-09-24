---
description: Finalise l'Axe en cours — commit, update project-state.md, archivage claude.md, génération du claude.md de l'Axe suivant.
allowed-tools: Read, Write, Bash(git *)
---

Finalisation de l'Axe en cours.

## Étape 1 — Vérification

Lire `claude.md` pour identifier le numéro de l'Axe en cours (N).
Vérifier les fichiers non commités : `git status`
Si des fichiers inattendus apparaissent : signaler et attendre confirmation.

## Étape 2 — Commit

Sélectionner explicitement les fichiers à inclure — **ne jamais utiliser `git add .`**
(risque d'ajouter des fichiers de travail, secrets, ou artefacts non voulus).

```bash
# 1. Lister les fichiers effectivement modifiés par l'Axe
git status --short

# 2. Ajouter les répertoires versionnés pertinents (source + docs + état)
git add src/ docs/ docs/agent/project-state.md claude.md axes/

# 3. Ajouter les nouveaux fichiers listés par git status, un par un
#    (ne pas ajouter les fichiers inattendus — signaler à l'utilisateur)
```

Générer un message de commit conventionnel depuis `claude.md` :
- Titre : `feat: Axe N - [titre de l'Axe]`
- Body : liste des changements principaux

```bash
git commit -m "[message généré]"
```

## Étape 3 — Mettre à jour project-state.md

Lire `docs/agent/project-state.md`.

Mettre à jour la section **"État courant"** :
- Dernier Axe complété : Axe N — [titre] — [date]
- Mettre à jour "Fonctionnalités actives" pour refléter ce qui vient d'être livré
- Mettre à jour "Contraintes techniques actives" si applicable
- Ajouter les nouvelles décisions [DT-00X] si applicable

Ajouter une entrée dans **"Historique des Axes"** :
- Périmètre livré (résumé 1-2 lignes)
- Changements structurants
- Sections specs devenues caduques (si applicable)
- Décisions prises
- Dette technique assumée (si applicable)

## Étape 4 — Archivage

Copier `claude.md` dans `axes/axe-N-[titre-court].md`.
Ne pas supprimer `claude.md` à la racine — l'utilisateur mergera.

## Étape 5 — Axe suivant

Lire `docs/plan-axes.md` pour identifier l'Axe N+1.

Si un Axe N+1 existe :
- Invoquer le skill `claude-md` avec l'argument N+1.
- Produire `claude.md` à la racine (écrase le précédent).
- Signaler : "claude.md Axe N+1 généré — à valider avant de passer en CLI.
  La branche axe-N est prête à merger."

Si dernier Axe :
- Laisser `claude.md` archivé, ne pas en générer un nouveau.
- Signaler : "Dernier Axe finalisé. Passer en Phase 5 — /livraison vX.Y.Z."

## Règle

Ne pas exécuter `git merge` ni `git checkout main` — laissé à l'utilisateur.
