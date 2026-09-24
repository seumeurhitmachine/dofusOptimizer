---
name: recette
description: Génère un PV de recette structuré pour l'Axe en cours à partir des scénarios Given/When/Then du claude.md.
---

# Skill — Recette

## Prérequis

- `claude.md` présent à la racine du repo (Axe en cours).

## Procédure

1. Lire `claude.md` et extraire le numéro d'Axe et tous les scénarios
   Given/When/Then de la section "Critères d'acceptation".
2. Produire `docs/recette-axe-N.md` avec la structure suivante :

```markdown
# Recette — Axe N : [Titre]

Date : YYYY-MM-DD
Testeur : 

| ID | Scénario | Attendu | Statut | Commentaire |
|---|---|---|---|---|
| SC-001 | [titre] | [résultat Then] | OK / KO | |
| SC-002 | ... | ... | | |
```

3. Laisser les colonnes Statut et Commentaire vides — remplies par l'humain.
4. Signaler que le PV est prêt dans `docs/recette-axe-N.md`.

## Règle

Un scénario = une ligne. Si un scénario Given/When/Then est complexe,
le décomposer en plusieurs lignes numérotées SC-00X.a, SC-00X.b.
