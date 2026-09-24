# Project State — Dofus Window Switcher

## État courant

> Mettre à jour à chaque /finalise. Max 30 lignes.

**Dernier Axe complété :** Axe 3 — Détection des fenêtres & comptes — 2026-09-24
**Phase :** Axes initiaux en cours (Axe 3 livré, Axe 4 à démarrer)

**Fonctionnalités actives :**
- (aucune US — socle infra) Solution `.slnx` + `src/App` (WPF) + `tests/App.Tests` (xUnit).
- Socle MVVM manuel (`ObservableObject`, `RelayCommand`), composition root `App.xaml.cs`
  (`ShutdownMode.OnExplicitShutdown`). Fenêtre à onglets (Comptes câblé, Raccourcis/Réglages vides),
  thème sombre (`Themes/Colors.xaml` + `Controls.xaml`).
- Publication vérifiée : `App.exe` single-file self-contained win-x64 démarre sans runtime.
- **Persistance (US-P01/P02/P03)** : modèle `Binding`/`AccountConfig`/`AppConfig` (schemaVersion 1),
  `JsonConfigStore` (source-gen `AppJsonContext`, écriture atomique, corruption→backup+défaut,
  migration montante). Autosave débouncé (`ConfigAutosaveService`). Config chargée au démarrage
  et injectée au `MainViewModel`. Invariant d'unicité globale porté par `AppConfig.HasBindingConflicts()`.
- **Détection (US-D01/D02)** : `IWindowDetector`/`WindowDetector` — `EnumWindows` initial +
  `SetWinEventHook` (CREATE/DESTROY/NAMECHANGE), événementiel sans polling. Reconnaissance via
  titre/classe `user32` seuls (`DofusWindowRecognizer`, extraction nom = fonction pure). Fusion
  détectés/persistés par clé `characterName` (`AccountMerge`, pure), absents conservés. Onglet
  Comptes temps réel (`AccountsViewModel` + `AccountItemViewModel` + `AccountsView`, lecture seule).
  `NativeMethods` étendu (P/Invoke détection). Détecteur démarré après `window.Show()`, libéré `OnExit`.

**Contraintes techniques actives :**
- .NET 10 + WPF, MVVM manuel, **zéro NuGet** dans l'app, publish single-file self-contained.
- C-02 : aucune interaction avec le processus DOFUS (API fenêtres `user32` seules).
- C-03 : aucune entrée synthétique.
- Threading : callbacks natifs → `Dispatcher` avant toute mutation UI ; I/O disque débouncée.
- Reconnaissance des clients DOFUS via titre/classe de fenêtre (pas de processus).
- Persistance : schemaVersion inférieure → migration montante idempotente ; supérieure ou
  corrompue → backup `*.corrupt-<horodatage>` + config par défaut, jamais de crash.
- État `connecté/absent` et `HWND` calculés au runtime, jamais persistés.

**Décisions structurantes en vigueur :**
- [DT-001] Pattern MVVM manuel léger (pas de framework), choisi pour l'UX/évolutions.
- [DT-002] Persistance JSON `%APPDATA%`, écriture atomique + autosave débouncé.
- [DT-003] Lancement manuel, app listée dans les applications Windows ; pas d'auto-démarrage par défaut.
- [DT-004] Namespace racine `DofusSwitcher` (assembly/exe reste `App.exe`) — évite le conflit
  namespace==classe `App` qui casse le point d'entrée WPF généré.
- [DT-005] Props de publication conditionnées à `RuntimeIdentifier != ''` dans `Directory.Build.props` :
  `build`/`test` restent framework-dependent, single-file uniquement au `publish -r win-x64`.
- [DT-006] Débounce de l'autosave logé dans un service dédié (`ConfigAutosaveService`, `DispatcherTimer`),
  pas dans la VM racine : `MainViewModel` reste sans type WPF ni threading (découpage en couches).
- [DT-007] `JsonConfigStore` accepte un chemin injectable (ctor `(string)`) pour la testabilité ;
  le ctor sans argument (`%APPDATA%`) reste le cas de production.
- [DT-008] Interop détection via **pointeurs de fonction** `delegate* unmanaged[Stdcall]<…>` +
  callbacks statiques `[UnmanagedCallersOnly]` : `[LibraryImport]` ne marshale pas les délégués
  (SYSLIB1051). `SetWinEventHook` n'ayant pas de user-data, le callback route vers l'unique instance
  via un champ statique (`WindowDetector` = singleton composition root). `<AllowUnsafeBlocks>` dans
  `App.csproj` seul (les tests restent sans `unsafe`).
- [DT-009] Vue runtime (`DetectedWindow`, `AccountRuntimeState`) séparée de la config persistée ;
  fusion pure `AccountMerge` = foyer testable. Comptes détectés non persistés affichés au runtime
  uniquement (aucune écriture config à cet Axe — gestion à l'Axe 4).

**Fichiers critiques — ne pas modifier sans discussion :**
- `src/App/Interop/NativeMethods.cs` (contraintes C-02/C-03).
- Propriétés de publication (`Directory.Build.props` / `.csproj`).
- `src/App/Persistence/AppJsonContext.cs` + schéma `AppConfig`.

---

## Historique des Axes

<!-- Une entrée par Axe complété. Ajoutée par /finalise. -->

### Axe 3 — Détection des fenêtres & comptes (2026-09-24)

**Périmètre livré :** P/Invoke détection dans `NativeMethods` (`EnumWindows`, `GetWindowText(Length)`,
`GetClassName`, `IsWindowVisible`, `SetWinEventHook`/`UnhookWinEvent`) ; `DofusWindowRecognizer`
(critère fenêtre DOFUS + extraction nom, purs) ; `AccountMerge` (fusion pure détectés/persistés) ;
`IWindowDetector`/`WindowDetector` (énumération initiale + WinEvent hook, marshaling Dispatcher) ;
vues runtime `DetectedWindow`/`AccountRuntimeState` ; `AccountsViewModel`/`AccountItemViewModel` +
`AccountsView` (onglet Comptes temps réel, lecture seule). Couvre US-D01/D02, RG-D01..D03/D05, C-02, PO-001.

**Changements structurants :** `NativeMethods` passe de squelette à premier point interop réel
(contraintes C-02/C-03 respectées : aucune inspection process) ; composition root instancie et démarre
le détecteur après `window.Show()`, le libère `OnExit` ; `MainViewModel(config, detector)` compose
désormais `AccountsViewModel`.

**Décisions :** [DT-008] pointeurs de fonction + `[UnmanagedCallersOnly]` (contournement SYSLIB1051)
avec routage par champ statique (singleton), `AllowUnsafeBlocks` limité à l'app ; [DT-009] séparation
vue runtime / config persistée, fusion pure testable, comptes détectés non persistés en runtime seul.
Critère fenêtre DOFUS fondé sur le motif de titre `" - Dofus"` (classe = signal secondaire relâché).

**Vérifications :** `dotnet build` 0 erreur / 0 warning ; `dotnet test` 37/37 (extraction nom, critère
fenêtre DOFUS, fusion apparition/disparition/conservation hors ligne/ordre, câblage VM Comptes) ;
`dotnet publish -r win-x64` single-file OK (pointeurs de fonction compatibles ReadyToRun).

**Dette technique assumée :** fonctionnement réel non testable unitairement (exige des clients DOFUS
ouverts) → à couvrir en recette. Réordonnancement/exclusion/persistance des comptes détectés → Axe 4.

### Axe 2 — Persistance & modèle de config (2026-09-24)

**Périmètre livré :** modèles `Binding`/`AccountConfig`/`AppConfig` (data-model, schemaVersion 1) ;
`IConfigStore`/`JsonConfigStore` (source-gen `AppJsonContext`, écriture atomique, corruption→backup+défaut,
migration montante) ; `ConfigAutosaveService` débouncé ; chargement au démarrage injecté au `MainViewModel`.
Couvre US-P01/P02/P03, RG-P01..P06, CA-04.

**Changements structurants :** matérialisation du schéma de config en code (le §Persistance du contexte
devient concret) ; composition root étendue (persistance → autosave → VM → flush `OnExit`) ; `MainViewModel`
prend désormais un `AppConfig` en constructeur + événement `ConfigChanged` (câblage pour l'édition future).

**Décisions :** [DT-006] débounce dans un service dédié (pas la VM racine) ; [DT-007] chemin injectable
dans `JsonConfigStore` pour la testabilité. Piège documenté : `AppConfig` record + `List<AccountConfig>`
→ égalité par référence sur la liste (non surchargée, comparaison structurelle côté tests).

**Vérifications :** `dotnet build` 0 erreur / 0 warning ; `dotnet test` 14/14 (round-trip CA-04, corruption→
backup+défaut, migration schéma inférieure/supérieure, unicité globale des `Binding`, défauts XButton2/1).

**Dette technique assumée :** autosave câblé mais sans émetteur `ConfigChanged` à cet Axe (aucun écran
d'édition avant l'Axe 4) ; export/import (US-P04) reporté à l'Axe 7.

### Axe 1 — Scaffolding & socle UI (2026-09-24)

**Périmètre livré :** solution + projets WPF/xUnit, `Directory.Build.props`, socle MVVM manuel,
composition root, fenêtre à 3 onglets vides, thème sombre, squelette interop, icône, tests smoke.

**Changements structurants :** mise en place complète de la stack .NET 10 / WPF / zéro-NuGet ;
publication single-file self-contained win-x64 opérationnelle (`App.exe` autonome).

**Décisions :** [DT-004] namespace racine `DofusSwitcher` ; [DT-005] props de publication
conditionnées au RID ; `Themes/Controls.xaml` séparé de `Colors.xaml` (seule source de HEX) ;
alias `Application` pour lever l'ambiguïté WPF/WinForms.

**Vérifications :** `dotnet build` 0 erreur ; `dotnet test` 3/3 ; `dotnet publish -r win-x64` →
`App.exe` (77 Mo) démarre sans crash ; zéro `PackageReference` dans `App.csproj`.

**Dette technique assumée :** `Resources/app.ico` est un placeholder généré (à remplacer).
