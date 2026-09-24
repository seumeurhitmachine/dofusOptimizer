---
description: Intègre une liste de bugs de recette et corrige l'Axe en cours.
argument-hint: [BUG-001 titre — attendu: X / observé: Y / repro: Z ...]
allowed-tools: Read, Write, Bash(git *)
---

Bugs de recette à corriger :

$ARGUMENTS

## Procédure

1. Lire `claude.md` pour le contexte de l'Axe en cours.
2. Lire `docs/agent/working-style.md`.
3. Pour chaque bug listé :
   - Analyser la cause.
   - Corriger.
   - Vérifier que les autres scénarios Given/When/Then du claude.md
     ne sont pas impactés par la correction.
   - Si la correction modifie le modèle de données ou un algo :
     mettre à jour `docs/data-model.md` ou `docs/algos/[nom].md`.
4. Mettre à jour la section corrections de `claude.md` (< 50 lignes total).
5. Ne pas modifier le périmètre de l'Axe — uniquement les corrections listées.
6. Résumer les corrections effectuées.
