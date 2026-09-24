<!-- INSTRUCTIONS CLAUDE
Ce fichier est un TEMPLATE de référence. Il fait partie du dossier Modèles
ajouté comme contexte permanent à tous les projets.

RÈGLES :
- NE JAMAIS modifier ce fichier.
- S'applique uniquement aux projets avec interface utilisateur.
- Workflow en 2 temps :
    1. Poser les questions de la section QUESTIONNAIRE à l'utilisateur.
    2. Sur la base des réponses, produire docs/ui-design.md + wireframes HTML
       dans docs/wireframes/.
- Si ce fichier n'est pas dans ton contexte, DEMANDE à l'utilisateur de te
  le fournir avant de produire quoi que ce soit.
- Générer APRÈS validation de la stack (§3.2) et du modèle de données (§3.3) dans docs/specs/
  (modèle de données).
- Le livrable docs/ui-design.md doit faire < 200 lignes.
-->

# Template UI Design

**Version :** 1.0

---

## Déclenchement

Cette phase s'applique si le projet comporte une interface utilisateur.
Elle se place entre la validation de docs/specs/ et la génération
de docs/plan-axes.md.

**Prompt pour démarrer :**

```
Le projet [Nom] a une interface utilisateur.
Lance la phase UI Design en suivant TEMPLATE-UI-DESIGN.md.
Pose-moi les questions du questionnaire.
```

---

## QUESTIONNAIRE

> Claude pose ces questions à l'utilisateur, section par section.
> Ne pas tout envoyer d'un coup — attendre les réponses avant de produire.

### Q1 — Ton visuel global

- Quel adjectif décrit le mieux l'interface visée ? (ex. épuré, dense, ludique, corporate, technique)
- Dark mode, light mode, ou les deux ?
- Une référence visuelle existante (app, site) que tu aimes pour ce projet ?

### Q2 — Palette

- Couleur primaire (action, CTA) : HEX ou description ?
- Couleur secondaire (accents, badges) : HEX ou description ?
- Couleur de fond principale : HEX ou description ?
- Couleur de fond secondaire (cartes, panneaux) : HEX ou description ?
- Couleur de texte principal : HEX ou description ?
- Couleur de texte secondaire (labels, placeholders) : HEX ou description ?
- États : couleur succès, erreur, warning, info ?

### Q3 — Typographie

- Police principale (titres) : nom ou "système" (SF Pro, Roboto, Inter...) ?
- Police de corps : même que titres ou différente ?
- Taille de base du texte courant (ex. 14px, 16px) ?

### Q4 — Navigation globale

- Quel est le pattern de navigation principal ? (tab bar, drawer, stack pur, combiné)
- Combien d'entrées dans la navigation principale ? Quels titres ?
- Y a-t-il des modales récurrentes ? Lesquelles ?
- Flux d'authentification : écran dédié avant navigation principale, ou intégré ?

### Q5 — Composants clés

- Le projet utilise-t-il des listes longues avec scroll infini ou pagination ?
- Y a-t-il des formulaires complexes (multi-étapes, upload, sélecteurs riches) ?
- Y a-t-il des composants spécifiques non standard à prévoir ? (carte map, graphiques, player...)
- Notifications push ou in-app prévues ?

### Q6 — Contraintes

- Y a-t-il une charte graphique existante à respecter (logo, couleurs imposées) ?
- Cibles : iOS uniquement, Android uniquement, ou les deux + web ?
- Contraintes d'accessibilité (WCAG AA minimum, tailles de police minimales) ?

---

## LIVRABLE 1 — docs/ui-design.md

> Produit par Claude sur la base des réponses. < 200 lignes.
> Ce document est la référence UI pour tout le développement.
> Il est lu par Claude à chaque Axe impliquant de l'UI.

```markdown
# [Nom du projet] — UI Design Reference

**Version :** 1.0
**Date :** YYYY-MM-DD
**Ref specs :** docs/specs/ — section stack

---

## Ton visuel

[Adjectif(s)] — [Light/Dark/Both] — Ref : [référence citée ou "aucune"]

## Palette

> Centralisée dans constants/colors.ts — ne jamais coder les valeurs en dur.

| Token          | HEX     | Usage                        |
|----------------|---------|------------------------------|
| primary        | #XXXXXX | CTA, boutons, liens actifs   |
| secondary      | #XXXXXX | Accents, badges              |
| background     | #XXXXXX | Fond principal               |
| surface        | #XXXXXX | Cartes, panneaux, inputs     |
| textPrimary    | #XXXXXX | Texte principal              |
| textSecondary  | #XXXXXX | Labels, placeholders         |
| success        | #XXXXXX | Succès, validation           |
| error          | #XXXXXX | Erreurs, champs invalides    |
| warning        | #XXXXXX | Avertissements               |
| info           | #XXXXXX | Informations neutres         |

## Typographie

| Rôle         | Police    | Taille | Poids |
|--------------|-----------|--------|-------|
| Titre H1     |           |        |       |
| Titre H2     |           |        |       |
| Corps        |           |        |       |
| Label        |           |        |       |
| Caption      |           |        |       |

## Navigation

Pattern : [tab bar / drawer / stack / combiné]

Entrées principales :
1. [Titre] — icône [nom]
2. [Titre] — icône [nom]
3. ...

Modales récurrentes : [liste ou "aucune"]
Auth : [écran dédié avant navigation / intégré]

## Composants clés

| Composant    | Notes                             |
|--------------|-----------------------------------|
| Listes       | [scroll infini / pagination / N/A]|
| Formulaires  | [simple / multi-étapes / N/A]     |
| Spécifiques  | [liste ou "aucun"]                |
| Notifications| [push / in-app / aucune]          |

## Contraintes

- Charte : [existante / aucune]
- Cibles : [iOS / Android / iOS + Android + Web]
- Accessibilité : [WCAG AA / aucune contrainte formelle]

## Wireframes

Voir docs/wireframes/ — un fichier HTML par écran clé.
```

---

## LIVRABLE 2 — docs/wireframes/[ecran].html

> Un fichier HTML par écran ou flux clé identifié dans les specs.
> Wireframe basse fidélité : structure et navigation, pas le style final.
> Utiliser uniquement HTML + CSS inline. Pas de framework externe.
> Représenter la navigation (liens entre écrans si possible via ancres/iframes).

Écrans à couvrir par défaut (adapter selon le projet) :
- Authentification (login / register)
- Écran principal de chaque entrée de navigation
- Toute modale récurrente
- Tout flux complexe identifié en Q5

---

## Règles d'utilisation pendant le développement

- Toujours lire docs/ui-design.md avant de coder un composant UI.
- Toutes les couleurs passent par `constants/colors.ts` — aucune valeur HEX en dur dans les composants.
- Les wireframes sont des références de structure, pas de pixel-perfect.
- Si un choix UI non couvert par ce document est nécessaire pendant un Axe,
  le signaler et attendre validation avant d'implémenter.
