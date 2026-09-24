---
name: livraison
description: Génère le changelog Keep a Changelog et met à jour le README pour une version donnée. Invoquer avec le numéro de version en argument.
argument-hint: [vX.Y.Z]
---

# Skill — Livraison

## Prérequis

- Tous les Axes de la version archivés dans `axes/`.
- Tag Git `$ARGUMENTS` fourni ou à créer.
- `CHANGELOG.md` existant ou à créer.

## Procédure

1. Lire tous les fichiers dans `axes/` pour reconstruire la liste des Axes
   livrés dans cette version.
2. Lire les messages de commit depuis le dernier tag :
   `git log [tag-précédent]..HEAD --oneline`
3. Produire ou mettre à jour `CHANGELOG.md` au format Keep a Changelog :

```markdown
## [$ARGUMENTS] - YYYY-MM-DD

### Added
- [fonctionnalités nouvelles issues des feat:]

### Fixed
- [corrections issues des fix:]

### Changed
- [modifications issues des refactor:]
```

4. Mettre à jour `README.md` : version courante, date, et si applicable
   les instructions d'installation ou les prérequis modifiés.
5. Créer le tag si non existant : `git tag $ARGUMENTS`
6. Confirmer : CHANGELOG mis à jour, README mis à jour, tag créé.

## Règle

Ne pas inclure les commits `docs:` et `chore:` dans le changelog public
sauf s'ils concernent des changements visibles pour l'utilisateur.
