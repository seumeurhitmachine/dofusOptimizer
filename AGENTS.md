# Instructions Codex

Ce projet suit la méthodologie du dossier `.codex/`.

## Lecture De Début De Session

Lire dans cet ordre avant toute action de cadrage ou de développement :

1. `.codex/methodo-ref.md`
2. `docs/agent/project-context.md` si présent — **lire en entier sa section
   `## Configuration verrouillée`** (versions infra, auth Zitadel, secrets Infisical) :
   elle fait foi, ne pas redemander ni re-déduire une info qui y figure.
3. `docs/agent/working-style.md` si présent
4. La section "État courant" de `docs/agent/project-state.md` si présent
5. `claude.md` si présent et si la tâche concerne un Axe
6. Les fichiers listés dans `claude.md` section `refs`

> Codex n'a pas de hook SessionStart : cette lecture est manuelle et obligatoire.

## Règles

- Les fichiers `.codex/commands/*.md` sont des procédures à interpréter quand l'utilisateur invoque une commande comme `/axe 1`.
- Les fichiers `.codex/skills/*/SKILL.md` contiennent les procédures détaillées ; les lire quand une command y renvoie.
- Ne jamais modifier `.codex/templates/` sauf demande explicite de faire évoluer la méthodologie du projet.
- Ne pas enchaîner deux phases sans validation explicite.
- Poser une question seulement si l'ambiguïté touche au périmètre, au comportement attendu ou à un choix irréversible.
- Les hooks Claude Code éventuellement présents dans les archis sont documentaires côté Codex ; ne pas supposer leur exécution automatique.
- `claude.md` garde son nom historique : c'est le pointeur machine de l'Axe en cours.
