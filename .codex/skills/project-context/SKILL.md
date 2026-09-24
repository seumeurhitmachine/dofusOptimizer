---
name: project-context
description: Génère les trois fichiers docs/agent/ — project-context.md, working-style.md,
  project-state.md (init). Déclencher après validation de docs/specs/.
---

# Skill — Project Context

## Prérequis

- `docs/specs/` validé (au minimum `docs/specs/_index.md`).
- `.codex/templates/TEMPLATE-PROJECT-CONTEXT.md` disponible.
- `.codex/templates/TEMPLATE-WORKING-STYLE.md` disponible.
- `.codex/templates/TEMPLATE-PROJECT-STATE.md` disponible.
- `docs/agent/archis/` peut contenir zéro ou plusieurs fichiers `*.md` décrivant des stacks.

## Procédure

1. Lire les trois templates intégralement.
2. Lire `docs/specs/_index.md` pour identifier la stack et les domaines.
3. **Résolution de l'archi** :
   a. Lister les fichiers `*.md` dans `docs/agent/archis/`.
   b. Pour chaque fichier, lire son en-tête YAML (`stack:`) et vérifier si la stack
      déclarée dans `_index.md` correspond (correspondance partielle tolérée,
      ex. "expo" matche "Expo + NativeWind + Prisma 5").
   c. Si exactement un fichier correspond : le lire et instancier ses conventions
      (arborescence, nommage, règles, flux auth, points d'attention…).
      Ne pas copier le fichier — l'instancier pour le projet.
   d. Si aucun fichier ne correspond : continuer sans archi spécifique,
      noter dans `project-context.md` qu'aucune archi prédéfinie n'a été trouvée.
   e. Si plusieurs fichiers correspondent : signaler l'ambiguïté à l'utilisateur
      et attendre qu'il précise avant de continuer.
4. Produire `docs/agent/project-context.md` — vérifier < 150 lignes.
   - Renseigner **intégralement** la section `## Configuration verrouillée` :
     versions réelles et figées (runtime, package manager, base, Caddy, PM2,
     Zitadel, Infisical, CI…) + colonne « Géré par ». Aucune cellule Version vide.
     Tirer les versions de l'archi active et/ou demander à l'utilisateur si inconnues.
5. Produire `docs/agent/working-style.md` depuis le template sans modification.
6. Produire `docs/agent/project-state.md` initialisé depuis le template.
7. Signaler à l'utilisateur. Ne pas enchaîner sans validation.

## Format attendu des fichiers d'archi

Chaque fichier dans `docs/agent/archis/` doit commencer par un front matter YAML :

~~~markdown
---
stack: Expo + NativeWind + Prisma 5
---
~~~

La clé `stack` est utilisée pour la résolution automatique (étape 3b).

## Règles

- `project-context.md` : table des matières + conventions uniquement.
  Pas de contenu de spec, pas d'architecture détaillée.
- `## Configuration verrouillée` obligatoire et complète : ne jamais livrer le
  fichier avec une cellule Version vide ni un placeholder `YYYY-MM-DD`.
- Auth = Zitadel (OIDC), secrets = Infisical : reporter issuer/realm et projet/env
  Infisical dans la section Configuration verrouillée.
- `working-style.md` : ne pas enrichir — rester < 20 lignes.
- `project-state.md` : initialiser avec "aucun Axe complété".
- Variables d'environnement : lister avec usage et exposition client (oui/non).
- Toujours inclure les migrations BDD dans "fichiers à ne pas modifier sans discussion".
