---
name: evolution
description: Prépare le projet pour une évolution post-version initiale. Archive les specs, consolide project-state.md, et démarre le premier Axe d'évolution. Invoquer une seule fois au passage en phase 6, puis utiliser le cycle normal /axe → /finalise.
argument-hint: [description courte de l'évolution — ex: "ajout notifications push"]
---

# Skill — Évolution post-version initiale

## Prérequis

Vérifier avant de continuer :
- Tag de version posé (`git tag --list` — doit contenir au moins `v1.0.0` ou équivalent).
- `axes/` contient les archives de tous les Axes initiaux.
- `CHANGELOG.md` à jour.

Si un prérequis manque : le signaler et attendre confirmation avant de continuer.

---

## Étape 1 — Archivage des specs (si pas encore fait)

Lire `docs/specs/_index.md`.

Si les specs ne sont pas encore marquées archivées :
- Pour chaque ligne du tableau, ajouter la colonne `Statut` avec la valeur
  `Archivé — Axe N` (identifier l'Axe depuis `axes/` qui a implémenté ce domaine).
- Sauvegarder `docs/specs/_index.md`.

Si les specs sont déjà archivées : passer à l'étape 2.

---

## Étape 2 — Consolidation de project-state.md

Lire `docs/agent/project-state.md` intégralement.

Vérifier la cohérence de la section "État courant" :
- "Fonctionnalités actives" reflète ce qui est réellement en production.
- "Contraintes techniques actives" est à jour.
- "Décisions structurantes en vigueur" ne contient pas de décisions obsolètes.
- Le dernier Axe complété correspond bien au dernier fichier dans `axes/`.

Si des incohérences sont détectées : les corriger et signaler ce qui a été mis à jour.
Si tout est cohérent : le confirmer.

---

## Étape 3 — Évaluation du besoin d'évolution

Analyser `$ARGUMENTS` pour déterminer le niveau de formalisation nécessaire.

**Évolution ciblée** (modification ou extension d'une feature existante) :
→ Pas de nouvelle spec. Passer directement à l'étape 4.

**Évolution structurante** (nouveau domaine fonctionnel, refonte majeure,
nouveau modèle de données significatif) :
→ Signaler à l'utilisateur : "Cette évolution semble structurante. Recommande
  une spec légère dans `docs/specs/[domaine]-v2.md` avant de générer le claude.md.
  Confirmer pour continuer sans spec, ou répondre `/cadrage` pour en créer une."
→ Attendre confirmation.

---

## Étape 4 — Génération du claude.md d'évolution

Identifier le numéro du prochain Axe (dernier Axe dans `axes/` + 1).

Lire `.codex/templates/INSTRUCTIONS-CLAUDE-MD.md`.
Lire les archis actives dans `docs/agent/archis/` uniquement pour les fichiers
touchés par l'évolution. Pour Expo/Prisma, sélectionner les fichiers spécialisés
utiles (`REPO`, `BACKEND`, `AUTH`, `PRISMA`, `FRONTEND`, `DOCKER`, `CICD`).
Ne pas lire le document d'audit migration sauf invocation explicite du skill `audit`.

Générer `claude.md` en mode **post-Axes initiaux** :
- Section `refs` : fichiers source concernés + `docs/data-model.md` si applicable.
  Ne pas référencer `docs/specs/` sauf si une spec v2 vient d'être créée.
- Section `refs` : ajouter les sections d'archi actives nécessaires à l'évolution.
- Section `tasks` : déduire depuis `$ARGUMENTS` et les fichiers source existants.
- Section `constraints` : vérifier `project-state.md` pour les décisions [DT-00X]
  actives pertinentes pour cet Axe.
- Section `acceptance` : Given/When/Then sur le comportement attendu de l'évolution.

Sauvegarder `claude.md` à la racine.

---

## Étape 5 — Confirmation

Résumer à l'utilisateur :
1. Specs archivées : oui / déjà fait
2. `project-state.md` : cohérent / corrections apportées (liste)
3. `claude.md` Axe N généré pour : [titre de l'évolution]

Message de sortie :
"Projet prêt pour la phase 6. Valider `claude.md`, puis lancer `/axe N` en CLI.
Cycle normal : `/axe N` → `/bugs` si besoin → `/finalise`."
