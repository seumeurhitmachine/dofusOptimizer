---
name: plan-axes
description: Génère docs/plan-axes.md en découpant les User Stories en Axes cohérents. Déclencher après validation de docs/agent/project-context.md.
---

# Skill — Plan des Axes

## Prérequis

- `docs/specs/` validé — en particulier `docs/specs/_index.md`.
- `docs/agent/project-context.md` validé.
- `.claude/templates/TEMPLATE-PLAN-AXES.md` disponible.

## Procédure

1. Lire `.claude/templates/TEMPLATE-PLAN-AXES.md` intégralement.
2. Lire `docs/specs/_index.md` pour identifier les domaines.
3. Lire les specs identifiées §US (User Stories) et §RG (Règles de gestion).
4. Lire les références d'architecture citées dans les specs concernées.
   Pour Expo/Prisma, charger uniquement les fichiers spécialisés utiles à chaque Axe.
5. Regrouper les User Stories en Axes selon les critères :
   - Dépendances techniques (auth avant tout Axe protégé)
   - Cohérence fonctionnelle (US du même écran/domaine ensemble)
   - Taille équilibrée : éviter les Axes < 2 US ou > 6 US
   - Complexité : un Axe élevé = une session 2-3h max, découper si nécessaire
6. Pour chaque Axe : renseigner périmètre (US + RG + refs specs), refs archis,
   ENF associées, prérequis, complexité estimée.
7. Produire la matrice de couverture. Vérifier que toutes les US Must et Should
   sont affectées. Signaler les US Could non couvertes comme "Reporté".
8. Sauvegarder dans `docs/plan-axes.md`.
9. Signaler à l'utilisateur. Ne pas enchaîner sans validation.

## Règles

- L'Axe 1 = scaffolding ou infrastructure de base (auth, BDD, navigation).
  Jamais de prérequis sur l'Axe 1.
- Axes Must avant Should, Should avant Could.
- Un Axe doit tenir dans un claude.md de < 80 lignes.
  Si la complexité dépasse, découper en deux Axes.
- Les références de specs utilisent le format `docs/specs/[domaine].md §X.Y`.
- Les références d'archis utilisent le format `docs/agent/archis/[archi].md §section`.
- Le document d'audit migration Expo/Prisma reste exclu du plan des Axes ordinaires.
