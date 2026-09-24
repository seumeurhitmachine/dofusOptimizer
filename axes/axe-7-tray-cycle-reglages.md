# Axe 7 : Tray, cycle de vie & réglages

refs: docs/plan-axes.md §Axe 7, docs/specs/tray.md §2.4/§2.5/§2.6/§3.x, docs/specs/persistence.md §2.4 (US-P04), docs/ui-design.md §Navigation/§Zone de notification, docs/wireframes/reglages.html, docs/wireframes/tray-menu.html, docs/agent/archis/ARCHI-DOTNET-WPF.md §Tray/§Composition root/§Publication, src/App/App.xaml.cs, src/App/ViewModels/MainViewModel.cs, src/App/Models/AppConfig.cs, src/App/Persistence/JsonConfigStore.cs, src/App/Services/SwitchCoordinator.cs
date: 2026-09-24

## tasks
- [ ] `TrayIconController` — `src/App/Tray/` — `NotifyIcon` (WinForms in-box) ; menu Ouvrir · Suspendre/Réactiver (case) · Quitter ; double-clic → affiche la fenêtre ; icône reflète l'état suspendu (archi §Tray)
- [ ] Cycle de vie — `App.xaml.cs` — `ShutdownMode.OnExplicitShutdown` ; fermer la fenêtre = `Hide` (RG-T01) ; « Quitter » → `Application.Shutdown` ; **lève l'intérim [DT-012]**
- [ ] Libération — `NotifyIcon` gardé en référence forte par `App` (sinon GC → icône fantôme) ; `Dispose` dans `OnExit` (RG-T06)
- [ ] Onglet Réglages — `src/App/Views/ReglagesView.xaml(.cs)` + `SettingsViewModel` — bascule suspendre, toggle démarrage Windows, export/import, à-propos ; via `MainViewModel.ConfigChanged` → autosave
- [ ] Suspension — bascule `AppConfig.InterceptionSuspended` (persistant, RG-T02/T03) ; déjà lue par `SwitchCoordinator.UpdateConfig` — tray et Réglages partagent la même source (pas de couplage UI→hook)
- [ ] Démarrage Windows (Could) — clé `HKCU\...\Run` **sans élévation** (ENF-005), off par défaut (RG-T05) ; jamais le dossier Startup ; abstraire derrière un service pour testabilité
- [ ] Export/import config (US-P04) — dialogues fichier ; réutilise `IConfigStore`/format (écriture atomique) ; import → valide, applique, autosave
- [ ] Câblage — composition root instancie `TrayIconController(window, mainViewModel)` ; `MainViewModel` expose `SettingsViewModel` ; onglet Réglages héberge `ReglagesView`
- [ ] Tests — `tests/App.Tests/` — bascule suspension persistée, export/import round-trip, toggle démarrage Windows (service registre fakable)

## constraints
- `ShutdownMode.OnExplicitShutdown` : la dernière fenêtre fermée ne quitte plus l'app (RG-T01) ; seule « Quitter » sort
- `NotifyIcon` = référence forte gardée par `App` ; `Dispose` garanti à la sortie (RG-T06)
- Démarrage Windows via `HKCU\...\Run` uniquement, sans élévation ; option off par défaut
- Export/import ne doit pas casser l'unicité globale des `Binding` ni le versioning `schemaVersion`
- Découvrabilité (US-T03/RG-T04) : raccourci menu Démarrer au packaging — lancement manuel, aucun auto-démarrage par défaut
- ref: project-state.md [DT-012] `ShutdownMode` intérimaire à lever cet Axe

## acceptance
Given l'app lancée When je ferme la fenêtre de configuration Then l'app continue et reste accessible via l'icône du tray (RG-T01)
Given l'icône du tray When je double-clique Then la fenêtre de configuration réapparaît
Given l'interception active When je choisis « Suspendre » (tray ou Réglages) Then aucune entrée n'est interceptée, l'icône reflète l'état, et c'est persistant (RG-T02/T03)
Given le menu du tray When je choisis « Quitter » Then l'app se ferme et l'icône est libérée sans fantôme (RG-T06)
Given une configuration When je l'exporte puis l'importe dans une instance vierge Then la configuration est restaurée (US-P04)
Given l'option démarrage Windows off par défaut When je l'active Then la clé `HKCU\...\Run` est posée (RG-T05)
