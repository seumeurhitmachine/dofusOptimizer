# Specs — Dofus Optimizer

**Profil sécurité :** minimal · **Interface utilisateur :** Oui
**Archi active :** `docs/agent/archis/ARCHI-DOTNET-WPF.md`
**Source fonctionnelle :** `docs/init/expression-de-besoin-dofus-switcher.md` (v0.2)
**Statut cycle documentaire :** Post-v1.0.0 — specs initiales **archivées** ; évolutions dans les specs `-v2`.

| Fichier | Domaine | Résumé | Statut |
|---|---|---|---|
| `overview.md` | **Principale** — cadrage & technique | Contexte, périmètre, contraintes C-01/02/03, ENF, C4, stack, sécurité socle, tests, traçabilité | Archivé — Axes 1→7 |
| `detection.md` | Détection & comptes | Détection fenêtres DOFUS, liste temps réel, ordre de rotation, exclusion, identité par nom de personnage | Archivé — Axe 3/4 |
| `switching.md` | Capture & bascule (cœur) | Hooks bas niveau, bascule cyclique/directe, interception conditionnée au focus, conflits, latence < 100 ms | Archivé — Axe 5/6 |
| `persistence.md` | Persistance config | JSON lisible `%APPDATA%`, autosave débouncé, reprise sur corruption, export/import | Archivé — Axe 2/7 |
| `tray.md` | Zone de notification & cycle de vie | Vie en tray, suspendre/réactiver l'interception, démarrage Windows optionnel | Archivé — Axe 7 |
| `comptes-v2.md` | **Évolution** — Comptes ↔ Personnages | Entité Compte (≤40 alphanum.), lien manuel personnage→compte, CRUD comptes, onglet Comptes en 3 zones, migration v1→v2 | **Actif — Axe 8** |

## Prochaines étapes (méthodologie)

1. **UI Design** (`/ui-design`) — l'application a une UI (fenêtre de config).
2. `/project-context` — instancier l'archi WPF détectée.
3. `/data-model init` — `docs/data-model.md` (schéma complet, voir esquisse dans `persistence.md`).
4. `/plan-axes` — découpage en Axes.

## Points ouverts

_Aucun ouvert._

- **PO-001** ✅ tranché — reconnaissance DOFUS via API fenêtres `user32` (titre/classe),
  sans inspection du processus (conforme C-02). Voir `detection.md`.
- **PO-002** ✅ tranché — lancement manuel, app listée dans les applications Windows
  (raccourci menu Démarrer), pas d'auto-démarrage par défaut. Voir `tray.md`.
