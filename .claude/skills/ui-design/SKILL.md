---
name: ui-design
description: Lance la phase UI Design — pose le questionnaire, attend les réponses, produit docs/ui-design.md et docs/wireframes/. Déclencher après validation des sections stack et modèle de données dans docs/specs/.
---

# Skill — UI Design

## Prérequis

- `docs/specs/` validé, notamment les sections stack et modèle de données dans la spec principale.
- `.claude/templates/TEMPLATE-UI-DESIGN.md` disponible dans le contexte.

## Procédure

### Étape 1 — Questionnaire

Poser les questions ci-dessous section par section.
Attendre les réponses complètes avant de passer à l'étape 2.
Ne pas poser toutes les sections d'un coup.

**Q1 — Ton visuel global**
- Quel adjectif décrit le mieux l'interface visée ? (ex. épuré, dense, ludique, corporate, technique)
- Dark mode, light mode, ou les deux ?
- Une référence visuelle existante que tu aimes pour ce projet ?

**Q2 — Palette**
- Couleur primaire (action, CTA) : HEX ou description ?
- Couleur secondaire (accents, badges) : HEX ou description ?
- Couleur de fond principale : HEX ou description ?
- Couleur de fond secondaire (cartes, panneaux) : HEX ou description ?
- Couleur de texte principal : HEX ou description ?
- Couleur de texte secondaire (labels, placeholders) : HEX ou description ?
- États : succès, erreur, warning, info ?

**Q3 — Typographie**
- Police principale (titres) : nom ou "système" ?
- Police de corps : même ou différente ?
- Taille de base du texte courant ?

**Q4 — Navigation globale**
- Pattern principal : tab bar, drawer, stack pur, combiné ?
- Combien d'entrées dans la navigation principale ? Quels titres ?
- Modales récurrentes ? Lesquelles ?
- Flux d'authentification : écran dédié ou intégré ?

**Q5 — Composants clés**
- Listes longues avec scroll infini ou pagination ?
- Formulaires complexes (multi-étapes, upload, sélecteurs riches) ?
- Composants non standard (carte map, graphiques, player...) ?
- Notifications push ou in-app ?

**Q6 — Contraintes**
- Charte graphique existante à respecter ?
- Cibles : iOS / Android / les deux + web ?
- Contraintes d'accessibilité ?

### Étape 2 — Produire docs/ui-design.md

Sur la base des réponses, produire `docs/ui-design.md` selon la structure
définie dans `.claude/templates/TEMPLATE-UI-DESIGN.md`. Plafond : 200 lignes.

Règles :
- Tous les tokens couleur exprimés en HEX dans la table Palette.
- La note "Centralisée dans constants/colors.ts" doit apparaître sous la table.
- Aucune valeur HEX en dur hors de ce fichier et de constants/colors.ts.

### Étape 3 — Produire docs/wireframes/

Un fichier HTML par écran clé identifié dans §2.4 (User Stories) dans docs/specs/ — spec principale.
Wireframe basse fidélité : structure et navigation uniquement, HTML + CSS inline,
pas de framework externe.

Écrans minimum : authentification, un écran par entrée de navigation principale,
modales récurrentes, flux complexes identifiés en Q5.

### Étape 4 — Validation

Signaler que les livrables sont prêts. Ne pas enchaîner sur project-context
sans validation explicite de l'utilisateur.
