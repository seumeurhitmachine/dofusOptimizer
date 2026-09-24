# Axe 8 : Comptes ↔ Personnages (modèle, CRUD, onglet 3 zones)

refs: docs/specs/comptes-v2.md, docs/data-model.md, docs/agent/archis/ARCHI-DOTNET-WPF.md §MVVM manuel/§Persistance/§Threading, src/App/Models/AppConfig.cs, src/App/Models/AccountConfig.cs, src/App/Persistence/AppJsonContext.cs, src/App/Persistence/JsonConfigStore.cs, src/App/ViewModels/AccountsViewModel.cs, src/App/ViewModels/SettingsViewModel.cs, src/App/ViewModels/MainViewModel.cs, src/App/Views/AccountsView.xaml, src/App/Views/ReglagesView.xaml, src/App/Services/AccountMerge.cs
date: 2026-09-24

## tasks
- [ ] Terminologie — renommer l'entité v1 « compte » en **personnage** (`AccountConfig` → `CharacterConfig`, clé `characterName`) ; ajouter `AccountName?` (lien nullable) — MAJ `docs/data-model.md`
- [ ] Modèle Compte — `src/App/Models/` — record `GameAccount(Name)` ; validateur pur `^[A-Za-z0-9 -]{1,40}$` + `Trim` non vide + unicité insensible casse (RG-C01)
- [ ] `AppConfig` — `src/App/Models/AppConfig.cs` — ajouter liste `Accounts` (comptes) ; `schemaVersion` → 2
- [ ] Migration v1→v2 — `JsonConfigStore`/`AppConfig` — montante idempotente : personnages conservés, **non liés**, réglages préservés (RG-C07)
- [ ] Sérialisation — `src/App/Persistence/AppJsonContext.cs` — enregistrer `GameAccount` + champ `AccountName`
- [ ] CRUD comptes — `SettingsViewModel` + `ReglagesView.xaml` — créer/renommer/supprimer ; suppression = cascade des personnages liés **sans confirmation** (RG-C04) ; via `MainViewModel` (seul writer) → autosave
- [ ] Liaison — `AccountsViewModel` — lier un personnage connecté à un compte **disponible** (filtre préfait : comptes sans personnage connecté, RG-C03) ; chemin unique de mutation
- [ ] Partition 3 zones — `AccountsViewModel` (pure, testable) — zone1 comptes connectés, zone2 personnages connectés sans compte, zone3 comptes déconnectés à personnages liés
- [ ] Onglet Comptes — `src/App/Views/AccountsView.xaml` — 3 zones : compte + personnage connecté en sous-titre (zone1), personnage + action « lier » (zone2), compte déconnecté (zone3)
- [ ] Tests — `tests/App.Tests/` — validation/unicité nom, partition 3 zones, cascade suppression, migration v1→v2 round-trip

## constraints
- Rotation/bascule **inchangée** : opère sur les personnages connectés (fenêtres) — RG-C05
- Appartenance **manuelle persistée**, aucune auto-détection (API `user32` seules) — RG-C06, C-02
- Suppression compte : perte des personnages liés **assumée**, sans confirmation — RG-C04
- Personnage persisté non lié + déconnecté : conservé en config, non affiché tant qu'absent (PO-C01)
- Migration montante idempotente, `schemaVersion` 2 ; corruption/schéma trop récent → backup + défaut — ref [DT-002]
- `MainViewModel` = seul writer — ref [DT-015] ; mutations via chemin unique — ref [DT-013]
- État connecté/absent + HWND calculés au runtime, jamais persistés

## acceptance
Given l'onglet Réglages When je saisis un nom valide (lettres/chiffres/espaces/tirets ≤40) unique Then le compte est créé et persisté
Given un compte à personnages liés When je le supprime Then compte + personnages liés retirés sans confirmation
Given un personnage lié connecté When j'ouvre l'onglet Comptes Then le compte est en zone1 avec le personnage en sous-titre
Given un personnage connecté sans compte When je le lie Then seuls les comptes sans personnage connecté sont proposés, puis il passe en zone1
Given un compte sans personnage connecté mais avec des liés When j'ouvre l'onglet Comptes Then le compte est en zone3
Given une config v1 (schemaVersion 1) When l'app charge Then les personnages sont conservés non liés, réglages préservés, schemaVersion 2
