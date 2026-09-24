<!-- INSTRUCTIONS CLAUDE
Référence méthodologique compacte. GUIDE-METHODOLOGIE.md est réservé à
l'utilisateur — ne pas le lire ni le modifier.

RÈGLES ABSOLUES :
- Ne jamais modifier les fichiers templates (`.claude/templates/`).
- Ne jamais produire un livrable sans avoir lu le template correspondant.
- Ne jamais enchaîner deux phases sans validation explicite.
- Si un template ou une archi requis est absent : le signaler, ne pas improviser.
- Poser une question si l'ambiguïté concerne le périmètre, le comportement
  attendu, ou un choix irréversible. Ne pas poser de question pour les
  détails d'implémentation — décider, ajouter un commentaire [DECISION], continuer.
- Pas de sous-agents sauf demande explicite de l'utilisateur.

Les procédures détaillées de chaque phase sont dans les skills (`.claude/skills/`).
Les slash commands (`.claude/commands/`) sont des entrées fines qui invoquent
le skill correspondant — toujours se référer au skill pour la procédure.
-->

# Référence Méthodologie — Claude

---

## Contexte permanent (lu à chaque session CLI)

**Obligatoire dès le début de session** (le hook SessionStart le rappelle) :

```
docs/agent/project-context.md    < 150 lignes — stack, §Configuration verrouillée, conventions, refs
docs/agent/working-style.md      < 20 lignes  — comportement agent
docs/agent/project-state.md      section "État courant" uniquement (< 30 lignes)
```

La section `## Configuration verrouillée` de project-context.md fait foi pour les
versions (runtime, package manager, base, Caddy, PM2), l'auth (Zitadel/OIDC) et les
secrets (Infisical). Ne pas redemander ni re-déduire une info qui y figure.

**Total cible : < 200 lignes. Ne jamais dépasser.**

---

## Phases et commandes

```
1. Intention        → clarification obligatoire avant production

2. Cadrage & Specs  → Cowork
   2.1 /cadrage <profil>    → docs/specs/[domaine].md + docs/specs/_index.md
   2.2 /ui-design           → si UI, après validation stack + data model
   2.3 /project-context     → docs/agent/project-context.md
                               docs/agent/working-style.md
                               docs/agent/project-state.md (init)
   2.4 /data-model init     → docs/data-model.md (initial)
   2.5 /plan-axes           → docs/plan-axes.md
   2.6 /claude-md 1         → claude.md (dernier livrable avant CLI)

3. Développement    → CLI, branche axe-N-titre
   /axe N           → lecture contexte permanent + claude.md + plan
   /bugs [liste]    → corrections post-recette
   /finalise        → commit + update project-state.md + archivage + claude.md suivant

4. Recette          → humain (/recette pour PV structuré)

5. Livraison        → /livraison vX.Y.Z

6. Évolution (post-Axes initiaux)
   /evolution [description]  → archivage specs + consolidation project-state.md
                               + claude.md de l'Axe d'évolution (une seule fois)
   /claude-md N              → Axes d'évolution suivants (cycle normal)
   /axe N → /finalise        → même cycle, refs vers fichiers source (pas les specs)

Audit :
   /audit [périmètre]        → rapport d'écarts et plan de migration, sans modifier le code

Commandes runtime (délèguent à l'archi active) :
   /dev   → section "Commandes de développement" de docs/agent/archis/<archi>.md
   /build → section "Build" ou "Release / publish" de l'archi
   /test  → section "Tests" de l'archi
```

Chaque étape de cadrage attend une validation avant de passer à la suivante.

---

## Règles contexte & tokens

- `project-context.md` < 150 lignes — table des matières, pas de contenu de spec
- `claude.md` < 80 lignes — format machine-readable, zéro prose
- `working-style.md` < 20 lignes — lu systématiquement, doit rester minimal
- `ui-design.md` < 200 lignes — lire avant tout Axe impliquant de l'UI
- `project-state.md` — seule la section "État courant" est en contexte permanent
- Les règles de constantes (couleurs, typographie, constantes stack) sont
  définies dans l'archi active (`docs/agent/archis/<archi>.md §Règles de constantes`),
  pas ici — elles sont stack-spécifiques.

---

## Commentaires orientés Claude dans le code

| Préfixe | Usage |
|---|---|
| `[ARCH]` | Contrainte architecturale — ne pas contourner sans discussion |
| `[ALGO]` | Pointer vers `docs/algos/[nom].md` |
| `[WARN]` | Piège non évident (thread safety, transaction, effet de bord) |
| `[DECISION]` | Arbitrage d'implémentation qui pourrait sembler étrange |

---

## Règles Git

- Branche par Axe : `axe-N-titre-court`
- Commits conventionnels : `feat:` `fix:` `refactor:` `docs:`
- Ne jamais utiliser `git add .` dans `/finalise` — sélectionner les fichiers explicitement
- Archiver `claude.md` dans `axes/axe-N-titre.md` après merge
- `claude.md` racine = Axe en cours uniquement

---

## Cycle de vie documentaire

**Axes initiaux :** specs dans `docs/specs/` = source de vérité fonctionnelle.
**Post-Axes initiaux :** specs archivées. Source de vérité = code + commentaires
`[ARCH][ALGO][WARN][DECISION]` + `docs/data-model.md` + `docs/agent/project-state.md`.
Claude ne relit plus les specs sauf si le `claude.md` de l'Axe le demande explicitement.

---

## Archi active

Chaque projet peut avoir zéro ou plusieurs fichiers d'archi dans `docs/agent/archis/*.md`. Une archi
expose :
- Un front matter YAML (`stack`, `aliases`, `category`, `language`).
- Une structure de repo et des conventions de nommage non négociables.
- Une section `## Hooks Claude Code`, si présente, fusionnée dans `settings.json` par le bootstrap.
- Une section `## Commandes de développement` utilisée par `/dev` `/build` `/test`.
- Une section `## Règles de constantes spécifiques` qui complète le contexte permanent.

Les règles de l'archi priment sur les conventions idiomatiques du langage.
Pour Expo/Prisma, lire l'index `ARCHI-EXPO-PRISMA.md` puis seulement les fichiers
spécialisés utiles. `ARCHI-EXPO-PRISMA-AUDIT-MIGRATION.md` est réservé à `/audit`.

---

## Contournements reconnus

- "sans formalisme" / "prototype rapide" → développer directement, ignorer la méthodologie
- "sans phase UI" / "sans plan des Axes" → skipper la phase indiquée, continuer normalement
