# Specs — Dofus Window Switcher

**Profil sécurité :** minimal · **Interface utilisateur :** Oui
**Archi active :** `docs/agent/archis/ARCHI-DOTNET-WPF.md`
**Source fonctionnelle :** `docs/init/expression-de-besoin-dofus-switcher.md` (v0.2)
**Statut cycle documentaire :** Actif (Axes initiaux) — specs = source de vérité.

| Fichier | Domaine | Résumé | Exigences |
|---|---|---|---|
| `overview.md` | **Principale** — cadrage & technique | Contexte, périmètre, contraintes C-01/02/03, ENF, C4, stack, sécurité socle, tests, traçabilité | ENF-01..06, CA-01..04 |
| `detection.md` | Détection & comptes | Détection fenêtres DOFUS, liste temps réel, ordre de rotation, exclusion, identité par nom de personnage | EF-01, 02, 03, 09, H-01 |
| `switching.md` | Capture & bascule (cœur) | Hooks bas niveau, bascule cyclique/directe, interception conditionnée au focus, conflits, latence < 100 ms | EF-04..08, C-01/02/03, ENF-02 |
| `persistence.md` | Persistance config | JSON lisible `%APPDATA%`, autosave débouncé, reprise sur corruption, export/import | EP-01..05 |
| `tray.md` | Zone de notification & cycle de vie | Vie en tray, suspendre/réactiver l'interception, démarrage Windows optionnel | EF-10, ENF-04 |

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
