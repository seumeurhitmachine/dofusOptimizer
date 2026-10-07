# Axe 13 : Compte chef + copie /invite

refs: src/App/Models/AppConfig.cs §LauncherPath/§MinimizeOnOpenSession (champ additif), src/App/Models/GameAccount.cs, src/App/ViewModels/MainViewModel.cs §SetAccountDirectBinding/§AddAccount/§DeleteAccount, src/App/ViewModels/SettingsViewModel.cs §RebuildAccountRows/§OnConfigChanged, src/App/ViewModels/GameAccountRowViewModel.cs, src/App/ViewModels/AccountsViewModel.cs §ConnectedRows/§RebuildZones, src/App/ViewModels/ConnectedRowViewModel.cs, src/App/Views/ReglagesView.xaml, src/App/Views/AccountsView.xaml §ConnectedRowTemplate, src/App/Services/IFileDialogService.cs + src/App/Views/FileDialogService.cs (modèle service UI), src/App/App.xaml.cs (composition root), docs/data-model.md §GameAccount/§AppConfig, src/App/App.csproj [DT-014][DT-015][DT-024][DT-025]
date: 2026-10-07

## tasks
- [ ] Champ chef persisté — `AppConfig` — `string? ChefAccountName { get; init; }` additif hors ctor positionnel (comme `LauncherPath`), défaut `null`, **unicité structurelle** (un seul chef) ; **pas de bump schemaVersion** (reste 3)
- [ ] Seul writer — `MainViewModel` — `SetChefAccount(string? name)` : `Config = Config with { ChefAccountName = toggle }` (rappuyer sur le chef courant → `null`) ; `DeleteAccount` doit **effacer** `ChefAccountName` si le compte supprimé était chef (pas de référence pendante) [DT-015]
- [ ] Service presse-papier — `IClipboardService.SetText(string)` dans `Services/` + impl WPF `ClipboardService` dans `Views/` (modèle `IFileDialogService`), VM **sans type WPF** [DT-014] ; câblé en composition root `App.xaml.cs`
- [ ] Couronne cliquable (Réglages) — `GameAccountRowViewModel` — `IsChef` (bool) + `ToggleChefCommand` (→ `MainViewModel.SetChefAccount`) ; `SettingsViewModel.RebuildAccountRows` passe `IsChef` (= `config.ChefAccountName == name`) et le rappel `setChef`
- [ ] Couronne dans la vue Réglages — `ReglagesView.xaml` — icône couronne cliquable **à gauche** du nom du compte, couleur accent (`PrimaryBrush`) si chef / atténuée (`TextSecondaryBrush`) sinon ; `Path` géométrie couronne dans les ressources (aucun HEX, brushes `Colors.xaml`)
- [ ] Résolution chef (Comptes) — `AccountsViewModel` §RebuildZones/ConnectedRows — marquer la ligne du **personnage connecté du compte chef** (`ConnectedRowViewModel.IsChef`) ; construire la liste des **autres** persos connectés (hors chef) pour la commande d'invite
- [ ] Indicateur couronne (Comptes) — `ConnectedRowViewModel` + `AccountsView.xaml §ConnectedRowTemplate` — `IsChef` → couronne (indicateur, non cliquable) **à droite** du nom du personnage ; jamais sur ligne anonyme/non liée
- [ ] Bouton /invite (Comptes) — `ConnectedRowViewModel.CopyInviteCommand` (visible ssi `IsChef`) + bouton icône « personne + » **à gauche de la croix** dans `ConnectedRowTemplate` ; copie `"/invite A; /invite B; ..."` = `string.Join("; ", autresPersosConnectés.Select(n => $"/invite {n}"))` via `IClipboardService`
- [ ] Version — `App.csproj` → `1.5.0`
- [ ] Tests + Docs — `tests/App.Tests/` (toggle chef remplace l'ancien / rappuyer efface ; `DeleteAccount` efface le chef ; round-trip `ChefAccountName` sans bump schéma ; formule /invite = autres persos connectés joints par `"; "`, chef exclu, 0 autre → chaîne vide) + `docs/data-model.md` §GameAccount/§AppConfig (champ `chefAccountName` additif)

## constraints
- `ChefAccountName` = **champ additif, défaut `null`, sans bump schemaVersion** (précédent `LauncherPath`/`MinimizeOnOpenSession`, `AppConfig.cs`) — jamais de migration
- **Un seul chef** porté par un champ unique (pas de bool par compte) → unicité structurelle, pas d'invariant à vérifier
- Chef = propriété d'un **compte** (`GameAccount`), jamais d'un perso non lié ni d'un client anonyme (Axe 11) ; couronne/invite dans Comptes requièrent le perso du chef **connecté**
- `MainViewModel` = **seul writer** de `AppConfig` [DT-015] ; VM **sans type WPF** → presse-papier via `IClipboardService` [DT-014]
- Presse-papier = texte local uniquement ; **aucune** interaction process/jeu ni entrée synthétique (C-02/C-03 intacts)
- Couleurs via brushes `Themes/Colors.xaml` (accent = `PrimaryBrush`) ; aucun HEX en dur

## acceptance
Given deux comptes When je clique la couronne du compte A dans Réglages Then A devient chef (couronne accent), un seul chef à la fois
Given A est chef When je clique la couronne de B Then B devient chef et A ne l'est plus
Given A est chef When je reclique la couronne de A Then plus aucun compte n'est chef
Given le personnage du compte chef est connecté When j'ouvre Comptes Then une couronne s'affiche à droite de son nom
Given le compte chef et 2 autres persos connectés When je clique le bouton « personne + » de la ligne chef Then le presse-papier contient `/invite Perso2; /invite Perso3` (chef exclu)
Given un compte chef seul connecté When je clique le bouton d'invite Then le presse-papier est vide (aucun autre perso)
Given un compte non chef When je regarde sa ligne Comptes Then pas de couronne ni bouton d'invite
Given je supprime le compte chef dans Réglages When je rouvre l'app Then aucun compte n'est chef (référence effacée)
Given un chef défini When je ferme puis rouvre l'app Then le même compte est toujours chef (persisté)
