# Axe 10 : Personnages sans compte de plein droit dans la rotation

refs: src/App/ViewModels/AccountsViewModel.cs §RebuildZones/§SetDirectBinding/§LinkCharacter, src/App/ViewModels/MainViewModel.cs §BuildRotationSnapshot, src/App/ViewModels/ShortcutsViewModel.cs §SyncDirectSlots/§Assignments, src/App/ViewModels/UnlinkedCharacterViewModel.cs, src/App/ViewModels/ConnectedAccountViewModel.cs, src/App/Views/AccountsView.xaml, src/App/Views/AccountsView.xaml.cs, docs/data-model.md §AccountConfig, docs/algos/rotation.md, docs/agent/project-state.md [DT-025][DT-026]
date: 2026-09-25

## tasks
- [ ] Liste connectés unifiée — `AccountsViewModel.RebuildZones` — fusionner zones 1&2 en une collection ordonnée par `Items` (liés + non liés) ; chaque ligne porte `Character` (œil/croix/DnD) + `AccountName?` + affordance liaison ; zone 3 (déconnectés) inchangée
- [ ] Ligne unifiée — VM de ligne connectée unique (refonte/fusion `ConnectedAccountViewModel`/`UnlinkedCharacterViewModel`) exposant `Character`, `AccountName?`, `AvailableAccounts`, `SelectedAccount`, `LinkCommand`, `CanLink`
- [ ] Template unique — `AccountsView.xaml` — un seul `ItemsControl`+`DataTemplate` : poignée ☰ + œil + croix pour tous ; bloc « compte : X » **ou** combo+« Lier » via `DataTrigger` sur `AccountName`
- [ ] Masquer « Lier » — `AccountsView.xaml` + ligne VM — `Visibility` du bouton liée à `SelectedAccount is not null` (aujourd'hui seulement grisé par `LinkCommand.CanExecute`)
- [ ] DnD toutes lignes — `AccountsView.xaml.cs` — `Handle_*`/`Row_DragOver` résolvent `.Character` de la ligne unifiée (retirer le cast strict `ConnectedAccountViewModel`) ; `MoveItem`/`PreviewReorder` opèrent déjà sur indices globaux `Items`
- [ ] Directs sans compte (snapshot) — `MainViewModel.BuildRotationSnapshot` — ajouter aux `directs` les `AccountConfig.DirectBinding` des persos connectés **sans** compte (aujourd'hui boucle `GameAccounts` seule)
- [ ] Directs sans compte (raccourcis) — `ShortcutsViewModel` — slot direct par perso connecté sans compte (clé=`characterName`, apply via nouveau chemin `Accounts.SetDirectBinding`) + inclusion dans `Assignments()` (conflit RG-S05)
- [ ] Transfert au lien — `AccountsViewModel.LinkCharacter` (ou `MainViewModel`) — `AccountConfig.DirectBinding` non nul → transféré au compte s'il n'en a pas, sinon effacé [DECISION]
- [ ] Tests — `tests/App.Tests/` — liste unifiée ordonnée, snapshot inclut direct d'un perso sans compte, conflit direct refusé, transfert/effacement au lien, gating visibilité « Lier », non-régression rotation next/prev sur non liés
- [ ] Docs — `docs/data-model.md` (réactivation `AccountConfig.DirectBinding` pour non liés) + `docs/algos/rotation.md` (slots incluent les non liés) + `project-state.md` [DT-029]

## constraints
- **Aucun bump `schemaVersion`** : `Excluded` et `AccountConfig.DirectBinding` existent et sont déjà dans l'unicité (`AppConfig.AllBindings`). Purement comportemental + UI
- Amende **[DT-025]** (directe portée par le compte) et **[DT-026]** (DnD zone 1 seule) → nouveau **[DT-029]** : directe **par perso** pour les non liés (`AccountConfig.DirectBinding`), **par compte** pour les liés (`GameAccount.DirectBinding`, inchangé) ; DnD sur toute la liste connectés
- VM sans type WPF/Dispatcher ; DnD = code-behind (§MVVM autorisé)

## acceptance
Given un perso connecté sans compte When j'ouvre Comptes Then il figure dans la liste connectés avec poignée ☰, œil et croix
Given un perso sans compte When je le glisse Then son rang de rotation change et est persisté
Given un perso sans compte non exclu When j'appuie suivant Then la rotation l'inclut (non-régression)
Given un perso sans compte When je l'exclus Then la rotation le saute
Given un perso connecté sans compte When je lui assigne un raccourci direct Then l'appui active sa fenêtre
Given une entrée déjà assignée When je l'assigne à un perso sans compte Then refus inline, aucune persistance
Given un perso sans compte avec DirectBinding When je le lie à un compte sans binding Then le binding est transféré au compte
Given la ligne d'un perso sans compte When aucun compte n'est sélectionné Then le bouton « Lier » est masqué
