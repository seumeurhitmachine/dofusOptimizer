# Axe 3 : Détection des fenêtres & comptes

refs: docs/specs/detection.md §2.4/§2.5/§3.x, docs/data-model.md §Conventions, docs/agent/archis/ARCHI-DOTNET-WPF.md §Interop/§Threading, src/App/Interop/NativeMethods.cs, src/App/Models/AccountConfig.cs
date: 2026-09-24

## tasks
- [ ] P/Invoke détection — `src/App/Interop/NativeMethods.cs` — `EnumWindows`, `GetWindowText`/`GetWindowTextLength`, `GetClassName`, `IsWindowVisible`, `SetWinEventHook`/`UnhookWinEvent` ; `SetLastError` où documenté (seul point interop, extension sanctionnée par cet Axe)
- [ ] Critère « fenêtre DOFUS » — méthode dédiée testable — titre et/ou classe `user32` uniquement, **aucune** inspection process (`GetWindowThreadProcessId`/`OpenProcess` interdits) — C-02, PO-001
- [ ] Extraction nom de personnage — fonction pure `string → string?` — testable seule (RG-D01, H-01)
- [ ] `IWindowDetector` — `src/App/Services/IWindowDetector.cs` — événements `AccountAppeared`/`AccountDisappeared` ; ignore ViewModel/UI (dépendances montantes par événement)
- [ ] `WindowDetector` — `src/App/Services/WindowDetector.cs` — `EnumWindows` initial + `SetWinEventHook` (`EVENT_OBJECT_CREATE`/`DESTROY`/`NAMECHANGE`) ; callbacks courts → `Dispatcher` ; événementiel, pas de polling (RG-D05)
- [ ] Vue runtime d'un compte — état `connecté/absent` + `HWND` calculés au runtime, jamais persistés (data-model §Conventions) ; distinct de `AccountConfig`
- [ ] Fusion détectés + persistés — clé `characterName` ; compte absent conservé (RG-D03) ; l'ordre persistant reste la source de vérité
- [ ] `AccountsViewModel` + item VM — `src/App/ViewModels/` — `ObservableCollection` mutée sur le thread UI uniquement (archi §Threading)
- [ ] Onglet Comptes — `src/App/Views/` — liste temps réel avec état (lecture seule cet Axe ; réordonnancement/exclusion → Axe 4)
- [ ] Câblage composition root — `src/App/App.xaml.cs` — instancier le detector, l'injecter à la VM, démarrer la détection après `window.Show()`
- [ ] Tests — `tests/App.Tests/` — extraction nom perso (pure), critère fenêtre DOFUS, fusion détectés/persistés (apparition/disparition, conservation hors ligne)

## constraints
- C-02 : reconnaissance **exclusivement** via API fenêtres `user32` (titre/classe) ; jamais d'inspection du processus DOFUS — PO-001
- Détection **événementielle** (`SetWinEventHook`), aucun polling actif — RG-D05 / ENF-003
- Callback natif minimal : capter puis republier sur le `Dispatcher` ; `UnhookWinEvent` + libération des handles à l'arrêt
- État `connecté/absent` et `HWND` : runtime uniquement, jamais écrits en config — ref: project-state.md
- Fusion par clé naturelle `characterName` (identité stable, conservation hors ligne)

## acceptance
Given deux clients DOFUS ouverts When l'outil observe les fenêtres Then chaque client apparaît comme compte détecté identifié par le nom du personnage (US-D01)
Given un compte configuré client fermé When j'ouvre son client Then il passe « absent » → « connecté » sans action manuelle (US-D02)
Given un client ouvert When je le ferme Then le compte passe à « absent » sans erreur
Given un titre de fenêtre DOFUS When extraction du nom Then le nom de personnage est isolé (RG-D01)
