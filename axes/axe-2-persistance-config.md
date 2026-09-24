# Axe 2 : Persistance & modèle de config

refs: docs/plan-axes.md §Axe 2, docs/specs/persistence.md §2.4/§2.5/§3.x, docs/data-model.md, docs/agent/archis/ARCHI-DOTNET-WPF.md §Persistance/§Threading
date: 2026-09-24

## tasks
- [ ] Modèle `Binding` — `src/App/Models/Binding.cs` — value object `record` : `kind` (Key|MouseButton) + `code` ; égalité par kind+code
- [ ] Modèle `AccountConfig` — `src/App/Models/AccountConfig.cs` — clé `characterName`, `excluded`, `directBinding?`
- [ ] Modèle `AppConfig` — `src/App/Models/AppConfig.cs` — `schemaVersion`, `interceptionSuspended`, `startWithWindows`, `nextBinding?`/`prevBinding?`, `accounts` (liste ordonnée) ; `Default` avec défauts XButton2/XButton1
- [ ] `AppJsonContext` — `src/App/Persistence/AppJsonContext.cs` — source-gen, camelCase, indenté, enums en chaînes
- [ ] `IConfigStore` — `src/App/Persistence/IConfigStore.cs` — `Load()` / `Save(AppConfig)`
- [ ] `JsonConfigStore` — `src/App/Persistence/JsonConfigStore.cs` — écriture atomique (temp + `File.Move`), corruption → backup `*.corrupt-<horodatage>` + défaut (RG-P06)
- [ ] Autosave débouncé — `DispatcherTimer` court réarmé à chaque modif (archi §Threading) — [DECISION] où loger le debounce (VM racine vs service)
- [ ] Chemins config — `src/App/Constants/AppConstants.cs` — `%APPDATA%\DofusSwitcher\config.json`
- [ ] Chargement au démarrage — `src/App/App.xaml.cs` — `configStore.Load()` en tête de composition root, injecté au `MainViewModel`
- [ ] Tests — `tests/App.Tests/` — round-trip save/reload, corruption → backup + défaut, migration `schemaVersion`, unicité globale des `Binding`

## constraints
- Unicité globale des `Binding` (nextBinding/prevBinding/directBinding) : fichier persisté toujours sans conflit — data-model §Invariants
- État `connecté/absent` et `HWND` calculés au runtime, jamais persistés — data-model §Conventions
- `schemaVersion` inférieure → migration montante idempotente ; supérieure → traitée comme illisible (backup + défaut)
- ref: project-state.md [DT-002] persistance JSON atomique + autosave débouncé
- Après modif du schéma : mettre à jour docs/data-model.md §Historique (working-style)

## acceptance
Given ordre + associations définis When modif de l'ordre puis redémarrage Then ordre et associations identiques (CA-04)
Given compte configuré When client fermé puis rouvert Then compte reprend sa place et son association (CA-03)
Given config.json corrompu When démarrage Then démarre config vide + copie `*.corrupt-<horodatage>` conservée
Given aucun fichier config When démarrage Then configuration vide par défaut (XButton2/XButton1)
