# Axe 11 : Clients DOFUS sans personnage + polish session/UI (v1.3.0)

refs: src/App/Services/DofusWindowRecognizer.cs, src/App/Services/WindowDetector.cs §EvaluateWindow/§Forget, src/App/Services/AccountMerge.cs, src/App/Models/DetectedWindow.cs, src/App/Models/AccountRuntimeState.cs, src/App/ViewModels/MainViewModel.cs §BuildRotationSnapshot, src/App/ViewModels/AccountsViewModel.cs §RebuildZones/§RefreshSessionState/§MaterializeFromItems/§OpenSession, src/App/ViewModels/ConnectedRowViewModel.cs, src/App/ViewModels/ShortcutsViewModel.cs, src/App/Views/AccountsView.xaml, src/App/Views/ReglagesView.xaml, src/App/ViewModels/SettingsViewModel.cs, src/App/Views/MainWindow.xaml(.cs), src/App/Interop/NativeMethods.cs, src/App/Models/AppConfig.cs, docs/data-model.md §AppConfig, docs/algos/rotation.md, docs/agent/project-state.md [DT-010][DT-025][DT-027][DT-028][DT-029]
date: 2026-09-26

## tasks
- [ ] Reconnaissance client sans perso — `DofusWindowRecognizer` — nouveau cas pur `Dofus <version> - Release` (2 segments, 1er == « Dofus » + version) → « client connecté sans personnage » ; distinct de `ExtractCharacterName` (≥4 segments). Reste **structurel** (ref [DT-010])
- [ ] Modèle runtime sans nom — `DetectedWindow`/`AccountRuntimeState` — porter l'absence de personnage (ex. `HasCharacter`/nom `null`) ; **identité = handle** (pas de nom stable), **JAMAIS persisté** (§ARCH DetectedWindow)
- [ ] Détection par handle — `WindowDetector` — `_known`/apparition/disparition des clients sans perso indexés **par handle** (pas par nom, éviter collision « Dofus ») ; NAMECHANGE : sans-perso→perso au login sur le même handle
- [ ] Fusion runtime-only — `AccountMerge.Merge` — ajouter les clients sans perso en fin (clé handle), connectés, **jamais rapprochés de `persisted` ni matérialisables**
- [ ] Rotation inclut sans perso — `MainViewModel.BuildRotationSnapshot` + `AccountsViewModel.MaterializeFromItems` — clients sans perso dans les slots (via détectés) **et exclus de la persistance** (pas d'écriture dans `_persisted`) et de tout `DirectBinding`
- [ ] Ligne « Dofus N » — `AccountsView.xaml` + `ConnectedRowViewModel` — variante client sans perso : libellé `Dofus N` (N = ordre de rotation) + **croix seule** ; pas de ☰/œil/combo/« Lier »/activation directe [DECISION]
- [ ] Isoler des raccourcis/comptes — `AccountsViewModel.RebuildZones` + `ShortcutsViewModel` — clients sans perso **hors** `ConnectedUnlinkedCharacters` (aucun slot direct, RG « pas de raccourci ») et hors `AvailableAccounts`/liaison
- [ ] Ouvrir une session — `AccountsViewModel.RefreshSessionState` — `ShowOpenSession` si **aucune fenêtre DOFUS ouverte** (`!HasConnectedClients`, clients sans perso inclus) ET `LauncherPath()` non nul ; retirer la condition `!launcherRunning` (launcher ouvert sans client → toujours proposé, l'action active sa fenêtre)
- [ ] Switch « Réduire à l'ouverture d'une session » — `AppConfig` (bool `MinimizeOnOpenSession`, additif, défaut `false`, **pas de bump schemaVersion**, comme `LauncherPath`) + `ReglagesView` sous « Ankama Launcher », **visible ssi `LauncherPath` non vide** + `SettingsViewModel` + `MainViewModel` setter ; `OpenSession` → si activé, minimiser la fenêtre (rappel vers `MainWindow`)
- [ ] Coins arrondis — `MainWindow.xaml.cs` + `NativeMethods` — `DwmSetWindowAttribute(DWMWA_WINDOW_CORNER_PREFERENCE = ROUNDSMALL)` sur le HWND (Win11) [DECISION] ; nouveau P/Invoke DWM (appel **UI seul**, conforme C-02/C-03 — fichier critique, justifier)
- [ ] Version — `App.csproj` → `1.3.0`
- [ ] Tests + Docs — `tests/App.Tests/` (recognizer nouveau format ; merge : client sans perso runtime-only **non persisté** ; rotation inclut un client sans perso ; gating `ShowOpenSession` ; switch persisté round-trip) + `docs/data-model.md` (`MinimizeOnOpenSession` + note « clients sans perso non persistés ») + `docs/algos/rotation.md` + `project-state.md` [DT-030]

## constraints
- **Clients sans personnage = runtime only** : identité = handle, jamais dans `AppConfig.Accounts` ; **ni raccourci, ni compte, ni exclusion/ordre persistés** [DECISION → DT-030]
- Reconnaissance **structurelle** (ref [DT-010]) : nouveau cas = 1er segment « Dofus » + version en avant-dernier ; pas de liste en dur
- **Aucun bump `schemaVersion`** (reste 3) : `MinimizeOnOpenSession` est un champ additif à défaut naturel `false`
- C-02/C-03 : le coin DWM est un appel d'affichage — **aucune** interaction process (extension de `NativeMethods`, fichier critique — justifier)
- VM sans type WPF/Dispatcher ; minimisation fenêtre + coins arrondis = `MainWindow` code-behind (§MVVM autorisé)

## acceptance
Given un client DOFUS à l'écran de sélection (titre « Dofus x.y.z - Release ») When j'ouvre Comptes Then il apparaît « Dofus 1 » dans la liste connectés, fermable
Given plusieurs clients sans personnage When je les regarde Then ils sont numérotés « Dofus 1, 2, 3… » et distincts (aucune collision)
Given un client sans personnage non exclu When j'appuie suivant Then la rotation l'inclut
Given un client sans personnage When j'ouvre les Raccourcis Then il n'a ni slot direct ni combo de compte
Given un client sans personnage When je ferme puis rouvre l'app Then il n'est jamais persisté en config
Given aucune fenêtre DOFUS ouverte et un launcher renseigné When j'ouvre Comptes Then « Ouvrir une session » est proposé (même launcher déjà ouvert sans client)
Given un client DOFUS ouvert (même sans personnage) When j'ouvre Comptes Then « Ouvrir une session » est masqué
Given le switch « Réduire à l'ouverture » coché When je clique « Ouvrir une session » Then l'application se minimise
Given un chemin de launcher vide When j'ouvre Réglages Then le switch « Réduire à l'ouverture » est masqué
Given l'application ouverte When je la regarde Then ses coins sont légèrement arrondis
