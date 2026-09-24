---
stack: Expo + Express + Prisma
topic: audit-migration
audience: agent
---

# Audit et migration vers le modele cible

Ce document guide un agent qui doit etudier une application existante, mettre en
evidence les ecarts avec le modele `ARCHI-EXPO-PRISMA`, puis proposer une
migration progressive vers l'homogeneisation totale des applications.

Objectif : produire un diagnostic factuel, priorise et actionnable. L'agent ne
doit pas melanger observation, decision et implementation.

## Entrees attendues

Avant de commencer, l'agent doit disposer de :

- l'application a auditer ;
- les fichiers `ARCHI-EXPO-PRISMA*.md` ;
- les specs projet si elles existent ;
- les fichiers `package.json`, `.env.example`, workflows, infra et Prisma ;
- les contraintes connues de production.

Si une information manque, l'agent note explicitement l'incertitude au lieu de
la combler par supposition.

## Livrable attendu

Le livrable doit contenir :

1. synthese de conformite ;
2. cartographie technique observee ;
3. tableau des ecarts au modele cible ;
4. risques associes ;
5. trajectoire de migration par phases ;
6. decisions a faire trancher par le proprietaire ;
7. quick wins possibles ;
8. points a ne pas modifier sans validation.

## Etape 1 - Inventaire du repo

Relever :

- structure racine ;
- presence ou absence de `packages/app` et `packages/api` ;
- package manager reel ;
- scripts racine ;
- version Node attendue ;
- presence de `pnpm-workspace.yaml` ;
- presence de Docker Compose ;
- presence de workflows GitHub ;
- presence d'infra PM2/Caddy/backup.

Sortie attendue :

```text
Structure observee:
- type: monorepo / app racine / autre
- package manager: ...
- app: ...
- api: ...
- prisma: ...
- infra: ...
```

## Etape 2 - App frontend

Comparer `packages/app` ou l'equivalent avec le standard.

Verifier :

- Expo Router ;
- plateformes `ios`, `android`, `web` ;
- client API centralise ;
- AuthContext ou equivalent ;
- stockage mobile des tokens ;
- absence d'appels `fetch` disperses ;
- separation composants UI / composants metier ;
- constantes design ;
- synchronisation Tailwind / tokens ;
- routes publiques et routes protegees ;
- routes admin.

Classer les ecarts :

- bloquant mobile ;
- dette d'homogeneisation ;
- simple difference de nommage.

## Etape 3 - Backend

Comparer le backend avec `ARCHI-EXPO-PRISMA-BACKEND.md`.

Verifier :

- backend Express separe ;
- routes sous `/api/v1` ;
- validation Zod ;
- middlewares `requireAuth`, `requireAdmin`, `errorHandler` ;
- organisation `modules/*` ;
- niveau de modularite reel ;
- presence de services metier testables ;
- integrations externes ;
- jobs, cron, Socket.IO si presents ;
- demarrage hors environnement test.

Pour chaque module important, relever :

```text
Module:
- routes:
- schemas:
- service:
- repository:
- tests:
- ecarts:
```

## Etape 4 - Authentification

Comparer avec `ARCHI-EXPO-PRISMA-AUTH.md`.

Verifier :

- JWT RS256 ;
- access token court ;
- refresh token opaque ;
- hash du refresh token en base ;
- rotation au refresh ;
- revocation au logout ;
- revocation globale au reset password ;
- stockage client mobile-ready ;
- gestion du `401` par le client API ;
- rate limit login ;
- reset password neutre et token hashe ;
- absence de secret expose dans `EXPO_PUBLIC_*`.

Noter clairement si l'app utilise :

- cookie httpOnly ;
- bearer token sans refresh ;
- refresh token signe ;
- refresh token brut stocke en base ;
- localStorage web uniquement ;
- AsyncStorage sans SecureStore.

Ces constats ne sont pas automatiquement des bugs, mais ils sont des ecarts a
traiter dans la trajectoire d'homogeneisation.

## Etape 5 - Prisma et base

Comparer avec `ARCHI-EXPO-PRISMA-PRISMA.md`.

Verifier :

- emplacement de `schema.prisma` ;
- singleton Prisma ;
- migrations versionnees ;
- seed ;
- indexes et contraintes ;
- cascades ;
- transactions sur operations multi-ecritures ;
- pagination serveur ;
- tests relies aux services Prisma critiques.

Points a relever :

```text
Prisma:
- emplacement:
- singleton:
- migrations:
- seed:
- risques schema:
- operations sans transaction:
- listes sans pagination:
```

## Etape 6 - Docker et portabilite

Comparer avec `ARCHI-EXPO-PRISMA-DOCKER.md`.

Verifier :

- `docker-compose.yml` ;
- PostgreSQL local ;
- Mailpit ou alternative si emails ;
- volumes nommes ;
- ports documentes ;
- `.env.example` exploitables ;
- procedure nouveau poste ;
- scripts `docker:up`, `docker:down`, `docker:logs`.

L'agent doit distinguer :

- ce qui est necessaire au developpement quotidien ;
- ce qui sert uniquement a reproduire la production ;
- ce qui doit rester specifique au projet.

## Etape 7 - CI/CD et exploitation

Comparer avec `ARCHI-EXPO-PRISMA-CICD.md`.

Verifier :

- CI avec pnpm ;
- typecheck ;
- lint ;
- tests ;
- build API ;
- build app ;
- migrations sur base de test si pertinent ;
- CD avec ordre correct ;
- `ecosystem.config.cjs` ;
- backup PostgreSQL ;
- documentation Caddy ;
- procedure de montee de version.

Ordre CD attendu :

1. pull ;
2. install ;
3. generate ;
4. build API ;
5. build app ;
6. migrate deploy ;
7. reload PM2.

Tout ordre different doit etre signale.

## Etape 8 - Tableau des ecarts

Produire un tableau priorise.

Format recommande :

| Domaine | Ecart | Standard cible | Impact | Priorite | Migration |
|---|---|---|---|---|---|
| Repo | npm workspaces | pnpm workspaces | Homogeneisation scripts | P2 | Convertir lockfile/scripts |
| Auth | Pas de refresh | Refresh token hashe | Sessions mobiles moins robustes | P1 | Ajouter table + endpoints |

Priorites :

- `P0` : risque securite, donnees ou production ;
- `P1` : bloque la convergence cible ;
- `P2` : dette importante mais migrable plus tard ;
- `P3` : nettoyage, nommage, confort agent.

## Etape 9 - Plan de migration

Le plan doit etre progressif et limiter les changements simultanes.

Ordre recommande :

1. stabiliser scripts et pnpm ;
2. ajouter Docker/dev environment ;
3. fiabiliser `.env.example` et validation env ;
4. ajouter ou corriger CI ;
5. ajouter PM2/CD/backup si absent ;
6. isoler ou normaliser `packages/api` ;
7. isoler ou normaliser `packages/app` ;
8. migrer auth vers access + refresh mobile-ready ;
9. harmoniser client API frontend ;
10. harmoniser Prisma/migrations ;
11. harmoniser design system ;
12. augmenter tests metier critiques.

Chaque phase doit contenir :

- objectif ;
- fichiers concernes ;
- risques ;
- verification ;
- rollback possible.

## Etape 10 - Decisions a faire trancher

L'agent doit laisser au proprietaire les decisions qui changent :

- comportement utilisateur ;
- strategie de session ;
- migration de donnees ;
- downtime ;
- domaine ou URLs publiques ;
- stockage de fichiers ;
- changement de schema majeur ;
- suppression de code historique ;
- ordre de priorite entre apps.

Les questions doivent etre formulees concretement :

```text
Decision:
- Situation observee:
- Option recommandee:
- Alternatives:
- Impact:
- Moment de decision:
```

## Gabarit de rapport

```md
# Audit migration - [Projet]

## Synthese

Statut global: conforme / partiellement conforme / divergent
Risque principal:
Migration recommandee:

## Cartographie observee

- Repo:
- App:
- API:
- Auth:
- Prisma:
- Docker:
- CI/CD:

## Ecarts prioritaires

| Priorite | Domaine | Ecart | Impact | Action proposee |
|---|---|---|---|---|

## Plan de migration

### Phase 1 - [Nom]

- Objectif:
- Changements:
- Verification:
- Risques:
- Rollback:

## Decisions a trancher

| Decision | Pourquoi | Options | Recommandation |
|---|---|---|---|

## Quick wins

- ...

## Zones a ne pas toucher sans validation

- ...
```

## Regles de prudence

- Ne pas proposer de big bang si une migration par phases est possible.
- Ne pas modifier l'auth et la structure repo dans la meme phase sauf projet
  neuf ou refonte acceptee.
- Ne pas migrer Prisma sans strategie de donnees.
- Ne pas remplacer des workflows de production sans verifier l'ordre reel de
  deploiement.
- Ne pas supprimer une route publique sans verifier ses consommateurs.
- Ne pas transformer un constat en decision sans le signaler.

## Sortie courte pour arbitrage

Quand le proprietaire veut d'abord trancher, fournir une version courte :

```text
Les 5 ecarts principaux:
1. ...
2. ...

Migration recommandee:
Phase 1: ...
Phase 2: ...
Phase 3: ...

Decisions a trancher:
1. ...
2. ...
```
