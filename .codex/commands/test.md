---
description: Lance la suite de tests définie par l'archi du projet.
argument-hint: [filtre optionnel — ex: nom du fichier de test]
allowed-tools: Bash
---

Exécuter la commande de test définie dans la section **Commandes de développement** de l'archi active.

Procédure :
1. Identifier l'archi active (`docs/agent/project-context.md §Stack` ou fichier unique dans `docs/agent/archis/`).
2. Lire le bloc `# Tests` ou `# Tests / qualité` de l'archi.
3. Exécuter la commande. Si un filtre est passé en $ARGUMENTS, l'ajouter selon la syntaxe du runner (ex : `pnpm vitest $ARGUMENTS` ou `dotnet test --filter $ARGUMENTS`).

Si la commande n'est pas documentée dans l'archi : le signaler.
