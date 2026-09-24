<!-- INSTRUCTIONS CLAUDE
Template de référence. NE JAMAIS modifier.
Produire : docs/agent/project-context.md dans le repo du projet.

RÈGLES :
- Limite stricte : < 150 lignes.
- Contient : stack, config verrouillée, arborescence, conventions, commandes, refs.
- Ne contient PAS : contenu de specs, architecture détaillée, règles métier.
- Section « Configuration verrouillée » OBLIGATOIRE : versions figées + mode de gestion.
  Ne jamais livrer ce fichier avec une cellule Version vide.
- Si stack Expo/Prisma : lire ARCHI-EXPO-PRISMA.md et instancier (ne pas recopier).
- Auth : Zitadel (OIDC). Secrets : Infisical. Voir l'archi active pour le détail.
-->

# [Nom du projet] — Contexte Projet

**Dernière mise à jour :** YYYY-MM-DD

---

## Configuration verrouillée

> Versions figées + mode de gestion. Source unique : évite répétitions et incompréhensions entre sessions.

| Composant | Technologie | Version | Géré par |
|---|---|---|---|
| Runtime | | | |
| Package manager | | | |
| Backend / Framework | | | |
| Frontend | | | |
| Base de données | | | |
| Auth | Zitadel (OIDC) | | issuer / realm |
| Secrets | Infisical | | projet / env |
| Reverse proxy | Caddy | | |
| Process manager | PM2 | | |
| CI/CD | GitHub Actions | | |
| Déploiement | | | |

---

## Arborescence

```
projet/
├── docs/
│   ├── agent/
│   │   ├── archis/             # Archi active si --archi précisé
│   │   ├── project-context.md  # Ce fichier — lu systématiquement
│   │   ├── working-style.md    # Comportement agent — lu systématiquement
│   │   └── project-state.md    # État courant + historique Axes
│   ├── specs/_index.md         # Index des specs (1 ligne par fichier)
│   ├── data-model.md           # ERD + invariants — source de vérité active
│   ├── algos/                  # Un fichier par algo complexe
│   ├── plan-axes.md
│   └── ui-design.md            # Si UI — lire avant tout Axe UI
├── axes/                       # claude.md archivés après merge
├── src/                        # Commentaires [ARCH][ALGO][WARN][DECISION]
└── claude.md                   # Axe en cours
```

---

## Conventions de code

### Nommage
> Conventions idiomatiques du langage par défaut. Documenter ici les spécificités projet.

### Organisation
> Feature-based | layer-based | autre

### Constantes
> Toutes dans `constants/` — jamais de valeur en dur (couleurs, URLs, tailles).

### Commentaires
> Commenter le "pourquoi", pas le "quoi".
> JSDoc/XML doc sur : composants exportés, hooks, fonctions utilitaires, handlers de routes.

### Patterns
> Repository | CQRS | MVC — préciser les patterns actifs sur ce projet.

---

## Base de données

> ORM/client, conventions nommage, politique migrations.
> Modèle de données complet : `docs/data-model.md`.
> Si Prisma 5 : singleton `server/db.ts` — ne pas instancier ailleurs.

---

## Authentification

> Zitadel (OIDC). Flux et validation JWKS détaillés dans l'archi active (§Auth).
> Noter ici uniquement : issuer, client(s), mapping des rôles propres au projet.

---

## Secrets & variables d'environnement

> Source unique = Infisical (dev + prod). `.env.example` = documentation des clés.
> Lister les variables et leur exposition client (`EXPO_PUBLIC_*` = jamais de secret).

| Variable | Usage | Exposée client ? |
|---|---|---|
| | | |

---

## UI Design

> Si UI : lire `docs/ui-design.md` avant tout composant.
> Toutes les couleurs via `constants/colors.ts` — aucune valeur HEX en dur.
> Supprimer cette section si pas d'UI.

---

## Commandes

```bash
# Dev
# Build
# Tests
# Migrations
```

---

## Dépendances principales

| Package | Usage |
|---|---|
| | |

---

## Fichiers à ne pas modifier sans discussion

> Migrations BDD, fichiers de config critiques, etc.

---

## Références

| Ressource | Chemin |
|---|---|
| Modèle de données | `docs/data-model.md` |
| État courant projet | `docs/agent/project-state.md` |
| Index specs | `docs/specs/_index.md` |
| UI Design | `docs/ui-design.md` (si UI) |
