# Axe 4 : Ordre & exclusion (onglet Comptes)

refs: docs/plan-axes.md §Axe 4, docs/specs/detection.md §2.4/§2.5, docs/ui-design.md §Composants/§Interactions, docs/data-model.md §AppConfig, docs/agent/archis/ARCHI-DOTNET-WPF.md §MVVM manuel, src/App/ViewModels/AccountsViewModel.cs, src/App/ViewModels/AccountItemViewModel.cs, src/App/Views/AccountsView.xaml, src/App/ViewModels/MainViewModel.cs
date: 2026-09-24

## tasks
- [ ] Commandes ↑/↓ — `src/App/ViewModels/AccountsViewModel.cs` — `RelayCommand` MoveUp/MoveDown ; bornes (haut/bas) via `CanExecute` ; répercussion immédiate sur `Items` (US-D03)
- [ ] Glisser-déposer — `src/App/Views/AccountsView.xaml(.cs)` — poignée `☰` (déjà au wireframe) ; réordonne `Items` ; [DECISION] handlers DnD = seule logique tolérée en code-behind (concern vue)
- [ ] Exclusion/réintégration — `AccountItemViewModel` (setter `IsExcluded` public) + commande `AccountsViewModel` — badge « exclu » déjà présent (RG-D04, US-D04)
- [ ] Persistance ordre + exclusion — `src/App/ViewModels/MainViewModel.cs` — muter `AppConfig.Accounts` (ordre liste = ordre rotation, source unique) et émettre `ConfigChanged` → autosave débouncé (Axe 2)
- [ ] Matérialisation compte détecté — à la 1re action (réordonner/exclure), ajouter le compte à `AppConfig.Accounts` s'il n'y est pas encore (jusqu'ici runtime only — ref [DT-009])
- [ ] Toolbar Comptes — `src/App/Views/AccountsView.xaml` — boutons ↑ Monter / ↓ Descendre / Exclure-réintégrer liés aux commandes (wireframe comptes.html)
- [ ] Préserver l'état runtime — réordonner/exclure ne doit pas casser `IsConnected`/`Handle` des items (pas de reconstruction à l'aveugle de la collection)
- [ ] Tests — `tests/App.Tests/` — MoveUp/MoveDown (bornes + effet), toggle exclusion, `ConfigChanged` émis avec le bon ordre, matérialisation d'un détecté à la 1re action

## constraints
- Ordre de `Accounts` = ordre de rotation, source unique (pas de champ `order`) — data-model §AppConfig
- Réordonnancement/exclusion → `ConfigChanged` → autosave débouncé — ref: project-state.md [DT-006]
- Compte détecté non persisté matérialisé en config seulement à la 1re action utilisateur — ref [DT-009]
- Exclusion = persister le flag `excluded` ; le saut effectif en rotation reste l'Axe 6 (pas ici)
- `ObservableCollection` mutée sur le thread UI uniquement ; unicité `characterName` préservée

## acceptance
Given trois comptes configurés When je déplace le 3e en 1re position Then l'ordre reflète immédiatement le nouvel arrangement et est persistant (US-D03)
Given un compte présent dans l'ordre When je l'exclus Then badge « exclu » + retiré de la rotation mais conservé et réactivable (US-D04, RG-D04)
Given un compte exclu When je le réintègre Then il redevient actif dans la liste
Given un réordonnancement When redémarrage de l'app Then l'ordre est conservé (persistance Axe 2)
