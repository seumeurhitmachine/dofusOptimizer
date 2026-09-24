---
name: cadrage
description: Génère docs/specs/[domaine].md + docs/specs/_index.md à partir du template 00-SPEC-CADRAGE-TEMPLATE.md. Déclencher après validation de la note d'intention et des questions de clarification.
---

# Skill — Cadrage & Specs

## Prérequis

- Note d'intention validée et questions de clarification répondues.
- `.codex/templates/00-SPEC-CADRAGE-TEMPLATE.md` disponible.
- Profil sécurité fourni par l'utilisateur : minimal | standard | renforcé.

Si l'un de ces éléments est manquant : le signaler et attendre.

## Profil sécurité

| Profil | Sections activées |
|---|---|
| minimal | Socle §3.7 uniquement |
| standard | Socle + EXT-S1 (classification données), EXT-S2 (audit log), EXT-S3 (chiffrement repos) |
| renforcé | Tout : socle + EXT-S1 à EXT-S5 (STRIDE, DAST, supply chain) |

## Procédure

1. Lire `.codex/templates/00-SPEC-CADRAGE-TEMPLATE.md` intégralement.
2. Identifier les domaines fonctionnels/techniques du projet.
   Exemples de découpage : `auth`, `api`, `data`, `notifications`, `admin`, `paiement`.
3. Produire un fichier `docs/specs/[domaine].md` par domaine identifié.
   Chaque fichier est une instanciation partielle du template — uniquement les
   sections pertinentes pour ce domaine.
4. Dans chaque spec, renseigner `Archi active` et `§3.2 Références d'architecture`
   avec les fichiers de `docs/agent/archis/` réellement applicables.
   Pour Expo/Prisma, référencer les fichiers spécialisés pertinents
   (`REPO`, `BACKEND`, `AUTH`, `PRISMA`, `FRONTEND`, `DOCKER`, `CICD`) selon le domaine.
5. Produire `docs/specs/_index.md` :
   ```markdown
   # Specs — [Nom du projet]
   | Fichier | Domaine | Résumé |
   |---|---|---|
   | auth.md | Authentification | JWT RS256, refresh, RBAC |
   | api.md | API REST | Endpoints, pagination, contrats |
   | ... | ... | ... |
   ```
6. Activer uniquement les extensions sécurité correspondant au profil.
   Supprimer les sections non activées des fichiers produits.
7. Ajouter "Interface utilisateur : Oui | Non" dans l'en-tête de la spec principale.
8. Signaler à l'utilisateur que les fichiers sont prêts pour relecture.
   Ne pas enchaîner sans validation explicite.

## Règles

- Un domaine = un fichier. Éviter les fichiers > 150 lignes.
- Les diagrammes Mermaid (C4, ERD) vont dans la spec du domaine le plus concerné.
  Le modèle de données complet sera dans `docs/data-model.md` — ne pas dupliquer.
- La note de renvoi vers `.codex/templates/TEMPLATE-UI-DESIGN.md` doit apparaître dans la spec
  principale si "Interface utilisateur : Oui".
- Le document d'audit migration Expo/Prisma ne doit jamais être référencé dans
  les specs ordinaires ; il est réservé au skill `audit`.
- Les specs sont la source de vérité pendant les Axes initiaux.
  Post-Axes initiaux, elles deviennent des archives — le signaler dans `_index.md`
  en ajoutant un statut : `Archivé — Axe N`.
