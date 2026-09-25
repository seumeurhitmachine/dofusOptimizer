# Axe 9 : Cycle de session & de vie — launcher, fermeture des clients, comportements fenêtre

refs: docs/data-model.md §AppConfig, docs/agent/project-state.md §État courant, docs/agent/archis/ARCHI-DOTNET-WPF.md §MVVM manuel/§Threading/§Composition root/§Interop, src/App/Interop/NativeMethods.cs, src/App/Services/ISessionProcessService.cs, src/App/ViewModels/AccountsViewModel.cs, src/App/ViewModels/SettingsViewModel.cs, src/App/ViewModels/MainViewModel.cs, src/App/Views/AccountsView.xaml, src/App/Views/ReglagesView.xaml, src/App/Views/MainWindow.xaml.cs, src/App/App.xaml.cs, src/App/Themes/Colors.xaml
date: 2026-09-25

## tasks
- [x] PID depuis HWND — `NativeMethods.cs` — surcharge `GetWindowThreadProcessId(out uint)` + `GetProcessIdFromWindow` (fenêtre→PID, **aucun** OpenProcess) ; `[ARCH]` exception C-02 [DT-027]
- [x] Service session — `ISessionProcessService` + `SessionProcessService` — `IsLauncherRunning`, `ResolveLauncherPath(configuredPath)` (config prime, sinon `%LOCALAPPDATA%\Programs\Ankama Launcher\…` + registre `App Paths`, sélection pure testable), `LaunchLauncher(path)`, `KillByHandle(hwnd)`. Fakable [DT-023]
- [x] Onglet Comptes — `AccountsViewModel` — `OpenSessionCommand` (visible ssi 0 client connecté ET launcher absent ET chemin résolu), `CloseClientCommand` par ligne connectée (zones 1&2), `EndSessionCommand` (kill tous + `Action` shutdown). Sans type WPF ; `SetLauncherPath` réévalue le gating
- [x] Chemin launcher configurable — `AppConfig.LauncherPath` (persisté) + champ + « Parcourir… » (`ReglagesView`) ; masqué si aucun chemin utilisable
- [x] Cycle de vie fenêtre — `AppConfig.CloseMinimizes` (défaut true) + `MinimizeToTray` + switches Réglages ; `MainWindow.OnClosing`/`OnStateChanged` (fermer minimise/quitte, minimiser en barre d'état) ; bouton « Fermer l'application » (visible si CloseMinimizes) ; sortie réelle pose `ForceClose` avant `Shutdown`
- [x] Ajout compte dépliable — `SettingsViewModel.IsAddAccountVisible` + `ShowAddAccountCommand` (idempotent) ; replié au changement d'onglet (`MainWindow.OnTabChanged`)
- [x] Migration v2→v3 — `JsonConfigStore.Migrate` — configs < v3 → `CloseMinimizes` forcé true (préserve le tray)
- [x] Divers — tray « Ouvrir la configuration » → « Ouvrir » ; exe → `DofusOptimizer.exe` (`AssemblyName`) ; version 1.1.0
- [x] Tests — `tests/App.Tests/` — service (résolution/gating/kill/end session), chemin configuré prioritaire, migration v3 + round-trip, add-account dépliable, switches persistés, quit

## constraints
- **Exception C-02 assumée [DT-027]** : `Process.Start` (launcher) + `Process.Kill` (clients) SEULES interactions process ; aucune injection/mémoire/entrée synthétique (C-03)/lecture du jeu. PID via API fenêtre, pas d'`OpenProcess` d'inspection
- `AccountsViewModel`/`SettingsViewModel` sans WPF/Dispatcher : shutdown via `Action` injectée, process derrière interface fakable [DT-023]
- Sortie réelle ⇒ `MainWindow.ForceClose = true` AVANT `Shutdown()` (sinon `OnClosing` l'annule quand « fermer minimise »)
- Aucune valeur HEX hors `Themes/Colors.xaml` (`PrimaryBrush`/`ErrorBrush`/`TextOnAccentBrush`)
- schemaVersion 2 → **3** ; migration montante idempotente ; l'initialiseur `init` ne survit pas au source-gen → défaut porté par la migration — ref [DT-002]

## acceptance
Given 0 client connecté, aucun « Ankama Launcher » et un chemin résolu When j'ouvre Comptes Then le bouton « Ouvrir une session » (fond accent, texte blanc) est affiché
Given aucun chemin utilisable When j'ouvre Comptes Then le bouton « Ouvrir une session » est masqué
Given un chemin configuré dans Réglages When je clique « Ouvrir une session » Then ce chemin est lancé (il prime sur l'auto-détection)
Given un client connecté When je clique sa croix rouge Then son processus est force-killé, les autres intacts
Given ≥1 client connecté When je clique « Terminer session » Then tous les clients sont force-killés puis l'app se ferme
Given « Fermer minimise » activé When je clique [X] Then la fenêtre se minimise (barre d'état si l'option est active) au lieu de quitter
Given une config v2 (sans cycle de vie) When l'app charge Then schemaVersion 3 et CloseMinimizes = true (comportement tray préservé)
