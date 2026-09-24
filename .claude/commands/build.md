---
description: Lance la commande de build / publish définie par l'archi du projet.
argument-hint: [cible optionnelle — ex: win-x64, android, web]
allowed-tools: Bash
---

Exécuter la commande de build définie dans la section **Commandes de développement** de l'archi active.

Procédure :
1. Identifier l'archi active (`docs/agent/project-context.md §Stack` ou fichier unique dans `docs/agent/archis/`).
2. Lire le bloc `# Build` (ou `# Release / publish`) de l'archi.
3. Exécuter la commande. Si $ARGUMENTS est une cible supportée, l'utiliser.

Si la cible n'est pas supportée ou la commande non documentée : le signaler.
