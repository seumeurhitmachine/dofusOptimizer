<!-- INSTRUCTIONS CODEX
Template de référence. NE JAMAIS modifier.
Produire : docs/agent/working-style.md dans le repo du projet.
Ce fichier est lu à chaque session — rester sous 20 lignes.
-->

# Working Style

No subagents unless user explicitly requests it.

Ask before acting if ambiguity concerns: scope, expected behavior, or irreversible choices.
For implementation details: decide, add a [DECISION] comment, continue.

Reasoning: minimal for fix/refactor. Unconstrained for architecture/design.

Never reproduce file content in responses — reference by path and section.
On uncertainty: one focused question, not a list.

After modifying data model: update docs/data-model.md before closing the task.
After modifying an algo: update docs/algos/[nom].md before closing the task.
