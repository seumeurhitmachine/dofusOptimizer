---
name: audit
description: Audite un projet existant contre l'architecture cible active et propose une trajectoire de migration priorisée. Ne modifie pas le code.
argument-hint: [objectif ou périmètre d'audit]
---

# Skill — Audit architecture

## Prérequis

- Projet existant accessible.
- Architecture cible présente dans `docs/agent/archis/` ou fournie par l'utilisateur.
- Specs projet lues seulement si elles existent et sont utiles au diagnostic.

Si une information manque, noter l'incertitude au lieu de la combler.

## Règle de lecture spéciale Expo/Prisma

Le fichier `.claude/skills/audit/references/ARCHI-EXPO-PRISMA-AUDIT-MIGRATION.md`
est réservé à ce skill. Ne jamais le lire pendant `/cadrage`, `/plan-axes`,
`/claude-md`, `/axe` ou `/evolution`.

Pour un audit Expo/Prisma, lire dans cet ordre :

1. `docs/agent/archis/ARCHI-EXPO-PRISMA.md`
2. Les fichiers spécialisés `REPO`, `BACKEND`, `AUTH`, `PRISMA`, `FRONTEND`,
   `DOCKER`, `CICD`, `CHECKLIST`
3. `.claude/skills/audit/references/ARCHI-EXPO-PRISMA-AUDIT-MIGRATION.md`

## Procédure

1. Inventorier la structure du repo, le package manager, les scripts, l'app,
   l'API, Prisma, Docker, CI/CD et l'infra.
2. Comparer le frontend à l'archi active : routing, client API, stockage tokens,
   séparation UI/métier, constantes de design, compatibilité web/mobile.
3. Comparer le backend : Express, `/api/v1`, Zod, middlewares, modules,
   services testables, jobs, intégrations.
4. Comparer l'auth : access token, refresh token, stockage, rotation,
   révocation, rate limit, reset password.
5. Comparer Prisma : emplacement, singleton, migrations, seed, indexes,
   transactions, pagination, tests DB.
6. Comparer Docker, CI/CD, PM2/Caddy/backup.
7. Produire un tableau des écarts priorisés.
8. Proposer une trajectoire de migration progressive avec vérification,
   risques et rollback par phase.
9. Lister les décisions à faire trancher par le propriétaire.
10. Créer le livrable dans `./audit/RAPPORT_YYYY-MM-DD.md`, en remplaçant
    `YYYY-MM-DD` par la date du jour.

Avant d'écrire le livrable, créer le dossier `./audit` s'il n'existe pas.
Utiliser le template
`.claude/skills/audit/references/RAPPORT-AUDIT-TEMPLATE.md` comme
structure de rapport. Remplacer tous les placeholders par les constats du projet
audité, supprimer les lignes inutiles, et ajouter autant de lignes de tableaux
ou de phases que nécessaire.

## Priorités

- `P0` : risque sécurité, données ou production.
- `P1` : bloque la convergence cible.
- `P2` : dette importante mais migrable plus tard.
- `P3` : nettoyage, nommage, confort agent.

## Livrable

Produire un rapport Markdown dans `./audit/RAPPORT_YYYY-MM-DD.md`.

Le rapport doit suivre la structure du template d'audit commun :

- front matter avec projet, date, stack observée, référence et type ;
- synthèse de conformité ;
- cartographie technique observée ;
- analyse frontend, backend, authentification, Prisma/base, infra et CI/CD ;
- tableau priorisé des écarts ;
- risques associés ;
- trajectoire de migration par phases ;
- décisions à faire trancher ;
- quick wins ;
- points à ne pas modifier sans validation ;
- incertitudes notées.

Le fichier final ne doit contenir aucun placeholder `{{...}}`.

## Prudence

- Ne pas mélanger audit et refactor massif.
- Ne pas modifier l'auth et la structure repo dans la même phase sauf refonte acceptée.
- Ne pas migrer Prisma sans stratégie de données.
- Ne pas transformer un constat en décision.
