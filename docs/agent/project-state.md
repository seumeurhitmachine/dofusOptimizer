# Project State — Dofus Window Switcher

## État courant

> Mettre à jour à chaque /finalise. Max 30 lignes.

**Dernier Axe complété :** Axe 7 — Tray, cycle de vie & réglages — 2026-09-24
**Phase :** Axes initiaux **terminés** (Axe 7 = dernier Axe livré) → Phase 5 Livraison (`/livraison`)

**Fonctionnalités actives :**
- (aucune US — socle infra) Solution `.slnx` + `src/App` (WPF) + `tests/App.Tests` (xUnit).
- Socle MVVM manuel (`ObservableObject`, `RelayCommand`), composition root `App.xaml.cs`.
  Fenêtre à onglets (Comptes câblé, Raccourcis/Réglages vides), thème sombre (`Themes/`).
- Publication vérifiée : `App.exe` single-file self-contained win-x64 démarre sans runtime.
- **Persistance (US-P01/P02/P03)** : modèle `Binding`/`AccountConfig`/`AppConfig` (schemaVersion 1),
  `JsonConfigStore` (source-gen `AppJsonContext`, écriture atomique, corruption→backup+défaut,
  migration montante). Autosave débouncé (`ConfigAutosaveService`). Config chargée au démarrage
  et injectée au `MainViewModel`. Invariant d'unicité globale porté par `AppConfig.HasBindingConflicts()`.
- **Détection (US-D01/D02)** : `IWindowDetector`/`WindowDetector` — `EnumWindows` initial +
  `SetWinEventHook` (CREATE/DESTROY/NAMECHANGE), événementiel sans polling. Reconnaissance du client
  **Unity** (`DofusWindowRecognizer`) : classe `UnityWndClass` + titre `Nom - Classe - Version - Type`
  (avant-dernier segment = version), nom = 1er segment ; extraction pure. Fusion détectés/persistés
  par clé `characterName` (`AccountMerge`, pure), absents conservés. Journal debug opt-in (`DetectionLog`,
  var `DOFUS_SWITCHER_DEBUG`). Détecteur démarré après `window.Show()`, libéré `OnExit`.
- **Ordre & exclusion (US-D03/D04)** : `AccountsViewModel` — commandes ↑/↓ + glisser-déposer (poignée
  `☰`, code-behind DnD) + exclusion/réintégration ; toute action matérialise l'ordre visible dans
  `AppConfig.Accounts` (ordre liste = rotation) et déclenche l'autosave via `MainViewModel.ConfigChanged`.
- **Capture & associations (US-S04/S05)** : cœur pur `InputCapture` (encodage souris — toute la souris,
  X1/X2=1/2 + gauche/droit/milieu=3/4/5 —, rejet combinaison/modificateur D-01, formatage lisible).
  Modale `CaptureInputWindow` (événements `PreviewKeyDown/MouseDown` de la fenêtre focalisée, **pas** de
  hook global) via l'abstraction `IInputCaptureService`. Onglet Raccourcis (`ShortcutsViewModel` +
  `ShortcutSlotViewModel` + `ShortcutsView`) : suivant/précédent + directes par compte, effacer, conflit
  inline (RG-S05, réutilise `HasBindingConflicts`, refus avant persistance). `MainViewModel` = seul
  writer de `Config` ; `DirectBinding` via chemin unique `AccountsViewModel.SetDirectBinding`.
- **Interception & bascule (US-S01/S02/S03)** : décision **pure** `SwitchController` (gate focus DOFUS par
  appartenance de handle, rotation cyclique avec saut absents/exclus, activation directe) — algo
  `docs/algos/rotation.md`. Capture globale `InputHook` (`WH_KEYBOARD_LL`+`WH_MOUSE_LL`, décode via
  `InputCapture`, callback minimal `[UnmanagedCallersOnly]`). Activation `WindowActivator`
  (`SetForegroundWindow` + repli `AttachThreadInput`, non synthétique). `SwitchCoordinator` = glue
  (pré-filtre set de bindings + suspension, snapshot via `MainViewModel.BuildRotationSnapshot` sur thread UI).
- **Tray, cycle de vie & réglages (US-T01/T02/T04, US-P04)** : `TrayIconController` (`NotifyIcon` WinForms, menu
  Ouvrir · Suspendre/Réactiver (case) · Quitter ; double-clic → fenêtre ; état suspendu reflété par info-bulle + case ;
  réf forte dans `App`, `Dispose` en `OnExit`). Cycle de vie : `ShutdownMode.OnExplicitShutdown`, fermeture fenêtre =
  masquer (drapeau `MainWindow.ForceClose`), sortie réelle via « Quitter » (`Application.Shutdown`). Onglet Réglages
  (`SettingsViewModel`/`ReglagesView`) : bascule suspension (partage la source du tray via `MainViewModel`), démarrage
  Windows (`IStartupRegistryService` → `HKCU\...\Run`, off par défaut, réconcilié au démarrage), export/import config
  (`IFileDialogService` + `ConfigImport` pur pour valider sans effet de bord ; import → `MainViewModel.ApplyImportedConfig`
  recharge comptes/raccourcis + autosave). `AccountsViewModel.LoadPersisted` pour le rechargement à l'import.

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
  uniquement (aucune écriture config avant l'Axe 4).
- [DT-010] Critère de reconnaissance **structurel** (client Unity) : classe `UnityWndClass` + titre à
  ≥ 4 segments dont l'avant-dernier est une version `X.Y…`, nom = 1er segment. Préféré à une liste de
  classes de personnage (fragile : locale FR/EN, ajouts). Format réel confirmé en recette (H-01 validé).
- [DT-011] Journal de détection opt-in (`DetectionLog`, var d'env `DOFUS_SWITCHER_DEBUG`) : diagnostic
  local par fichier, zéro dépendance, coût nul désactivé. A servi à découvrir le vrai format de titre.
- [DT-012] `ShutdownMode.OnLastWindowClose` **intérimaire** : tant que le tray n'existe pas (Axe 7),
  fermer la fenêtre doit quitter le process (sinon fantôme + `OnExit` jamais exécuté). L'Axe 7
  rétablira `OnExplicitShutdown` + « fermer = masquer dans le tray ».
- [DT-013] Toute action ordre/exclusion **matérialise l'ordre visible complet** dans `AppConfig.Accounts`
  (ordre affiché = ordre persisté), matérialisant les comptes seulement détectés ; `DirectBinding`
  préservé par clé. La détection seule ne persiste jamais.
- [DT-014] Capture via abstraction `IInputCaptureService` (impl WPF `InputCaptureService` côté Views) :
  les ViewModels restent sans type WPF et testables avec un faux service. La modale n'utilise que les
  événements d'entrée de la fenêtre focalisée (jamais de `SetWindowsHookEx` — réservé Axe 6, anti-bot).
- [DT-015] `MainViewModel` = **seul writer** de `AppConfig`. Suivant/précédent édités top-level ; les
  `DirectBinding` passent par le chemin unique `AccountsViewModel.SetDirectBinding` (sinon un
  réordonnancement ultérieur écraserait le binding via `MaterializeFromItems`). Conflit détecté dans le
  VM (réutilise `HasBindingConflicts`), refus avant persistance ; réassigner à soi-même ≠ conflit.
- [DT-016] Lignes « activation directe » sourcées de `AppConfig.Accounts` (comptes persistés/matérialisés),
  pas du détecteur : un compte seulement détecté apparaît après une action sur l'onglet Comptes (DT-013).
- [DT-017] Gate « focus DOFUS » par **appartenance de handle** (le foreground est un client DOFUS ssi son
  HWND est parmi les handles connectés du détecteur) : réutilise la détection, aucun relecture titre/classe
  dans le hot path, ignore le launcher/écran de sélection (non nommés → non connectés).
- [DT-018] Hook bas niveau installé sur le **thread UI** : le callback lit directement `Accounts.Items`
  (handles/ordre/exclusion) sans marshaling, synchrone (latence). Pré-filtre O(1) (set des bindings +
  `InterceptionSuspended`) avant tout instantané/décision (RG-S06). Décision isolée dans `SwitchController` pur.
- [DT-019] `AttachThreadInput` (repli d'activation) utilise `GetWindowThreadProcessId` pour l'**identifiant
  de thread seul** — aucun `OpenProcess`/handle process : conforme C-02/C-03 (switching.md §3.x). `[WARN]`
  de `NativeMethods` rescopé (prohibition d'inspection process = *reconnaissance*, PO-001).
- [DT-020] `ShutdownMode.OnExplicitShutdown` + « fermer = masquer » (`MainWindow.ForceClose`) : **lève [DT-012]**.
  L'app vit dans le tray ; seule « Quitter » (menu tray) appelle `Application.Shutdown` → `OnExit` (unhook + flush +
  `Dispose` tray). Le `NotifyIcon` est gardé en **référence forte** par `App` (sinon GC → icône fantôme, RG-T06).
- [DT-021] Import de config (US-P04) via `ConfigImport.TryParse` **pur, sans effet de bord** — distinct de
  `JsonConfigStore.Load` (dont le backup+défaut silencieux effacerait la config sur fichier invalide). Un fichier
  illisible/schéma trop récent/en conflit est **refusé** (message inline), jamais appliqué. Application en bloc par
  `MainViewModel.ApplyImportedConfig` (remplace la config, `AccountsViewModel.LoadPersisted`, autosave).
- [DT-022] Reflet de l'état suspendu par **info-bulle + case du menu tray** (texte, conforme ui-design « jamais par la
  seule couleur ») ; une icône grisée distincte est **différée** (un seul `.ico` livré) pour éviter la gestion d'un
  HICON GDI runtime (surface de bug RG-T06).
- [DT-023] `AppConfig.StartWithWindows` = **intention persistée** ; le registre `HKCU\...\Run` est réconcilié au
  démarrage par la composition root (`IStartupRegistryService.SetEnabled(config.StartWithWindows)`) → re-pointe l'exe
  courant (chemin changé/réinstallation). Service abstrait derrière interface, **fakable** (tests VM sans registre réel).

**Fichiers critiques — ne pas modifier sans discussion :**
- `src/App/Interop/NativeMethods.cs` (contraintes C-02/C-03).
- Propriétés de publication (`Directory.Build.props` / `.csproj`).
- `src/App/Persistence/AppJsonContext.cs` + schéma `AppConfig`.

---

## Historique des Axes

<!-- Une entrée par Axe complété. Ajoutée par /finalise. -->

### Axe 7 — Tray, cycle de vie & réglages (2026-09-24)

**Périmètre livré :** `TrayIconController` (`NotifyIcon` WinForms : menu Ouvrir · Suspendre/Réactiver (case) ·
Quitter, double-clic → fenêtre, état suspendu reflété par info-bulle + case) ; cycle de vie
`OnExplicitShutdown` + « fermer = masquer » (`MainWindow.ForceClose`), sortie via « Quitter » ; onglet Réglages
(`SettingsViewModel`/`ReglagesView`) : suspension partagée avec le tray, démarrage Windows (`HKCU\...\Run`, off
par défaut), export/import de config. Couvre US-T01/T02/T04, US-P04, RG-T01..T06.

**Changements structurants :** `MainViewModel` expose `Settings` et gagne les chemins d'écriture
`SetInterceptionSuspended`/`SetStartWithWindows`/`ApplyImportedConfig` (reste seul writer) ; `RaiseConfigChanged`
rafraîchit aussi Réglages. Nouveaux services `IStartupRegistryService` (registre `Run`) et `IFileDialogService`
(dialogues WPF isolés côté Views, ref [DT-014]) ; `Persistence/ConfigImport` (validation pure). `AccountsViewModel`
gagne `LoadPersisted` (import). Composition root : `OnExplicitShutdown`, réconciliation registre au démarrage, tray
en référence forte, `Dispose` en `OnExit`. Onglet Réglages câblé dans `MainWindow`.

**Décisions :** [DT-020] `OnExplicitShutdown` + fermer=masquer (lève [DT-012]) ; [DT-021] import pur sans effet de
bord ; [DT-022] reflet suspension info-bulle+menu (icône grisée différée) ; [DT-023] `StartWithWindows` intention
persistée, registre réconcilié au démarrage, service fakable.

**Vérifications :** `dotnet build` Debug + Release 0/0 ; `dotnet test` 98/98 (11 nouveaux : round-trip export/import,
rejet illisible/schéma trop récent/conflit + migration, bascule suspension persistée, démarrage Windows via fake,
import applique+recharge, import invalide/annulé). `dotnet publish -r win-x64` → `App.exe` single-file (77 Mo).

**Dette technique assumée :** cycle de vie réel (fermer=masquer, « Quitter » libère l'icône sans fantôme) et
pose/retrait réel de la clé `HKCU\...\Run` non testables unitairement → recette. Icône grisée de suspension différée
([DT-022]) : reflet actuel par info-bulle + case du menu. Dernier Axe initial → passage en Phase 5 (`/livraison`).

### Axe 6 — Interception & bascule de focus (cœur) (2026-09-24)

**Périmètre livré :** décision pure `SwitchController` (gate focus DOFUS, rotation cyclique saut
absents/exclus, activation directe — `docs/algos/rotation.md`) ; capture globale `InputHook`
(`WH_KEYBOARD_LL`+`WH_MOUSE_LL`) ; activation `WindowActivator` (`SetForegroundWindow` + repli
`AttachThreadInput`) ; `SwitchCoordinator` (pré-filtre + suspension). Couvre US-S01/S02/S03, RG-S01..S03/S06,
C-01, CA-01/CA-02/CA-03.

**Changements structurants :** `NativeMethods` étendu (hooks LL + activation ; `[WARN]` rescopé pour
`GetWindowThreadProcessId` = thread id d'`AttachThreadInput`, conforme C-02). `MainViewModel` expose
`BuildRotationSnapshot()` (thread UI). Composition root câble activator/controller/coordinator/hook, hook
démarré après `Show()`, libéré `OnExit`. Ferme la boucle « capture (Axe 5) → décision → focus ».

**Décisions :** [DT-017] gate focus par handle ; [DT-018] hook sur thread UI + pré-filtre O(1), décision
pure isolée ; [DT-019] `AttachThreadInput` conforme C-02 (thread id seul).

**Vérifications :** `dotnet build` Debug + Release 0/0 ; `dotnet test` 87/87 (14 nouveaux : CA-01 cycle,
CA-03 absent sauté, exclu sauté, CA-02 non-DOFUS, C-01 consommé, directe même exclu, un seul présent,
prev symétrique, coordinateur pré-filtre/suspension/routage). `dotnet publish -r win-x64` → `App.exe` (77 Mo).

**Correctifs post-recette :** (1) crash au démarrage — l'export réel est `SetWindowsHookExW` (pas
`SetWindowsHookEx`) ; entry point corrigé (non couvert par les tests, chemin `OnStartup`). (2) bascule
nécessitant deux appuis — `SetForegroundWindow` renvoie vrai sans activer (verrou de premier plan) : le
court-circuit sautait le repli ; `WindowActivator` attache désormais **systématiquement** la file
d'entrée du thread au premier plan (+ `BringWindowToTop`) → activation en un seul appui.

**Dette technique assumée :** hooks LL + activation réelle (focus, `AttachThreadInput`, latence < 100 ms)
non testables unitairement → recette avec clients DOFUS réels. Suspension d'interception câblée (lue par
le coordinateur) mais sans UI de bascule ni persistance déclenchée par l'utilisateur → onglet Réglages Axe 7.

**Limitation connue :** seuls les boutons souris **standard Windows** (gauche/droit/milieu, X1/X2) passent
par `WH_MOUSE_LL`. Les boutons vendeur non standard (ex. bouton haut « DPI » de la Logitech MX Vertical)
sont gérés par le firmware/Logitech Options+ et n'atteignent pas le hook → non captables tant qu'ils ne
sont pas remappés vers une touche/bouton standard. À documenter en livraison.

### Axe 5 — Capture d'entrées & associations (2026-09-24)

**Périmètre livré :** cœur pur `InputCapture` (encodage souris étendu à toute la souris, rejet
combinaison/modificateur D-01, formatage lisible) ; modale `CaptureInputWindow` écoutant les événements
d'entrée de la fenêtre focalisée (pas de hook global) via `IInputCaptureService` ; onglet Raccourcis
(`ShortcutsViewModel`/`ShortcutSlotViewModel`/`ShortcutsView`) — associations suivant/précédent +
directes par compte, effacer, conflit inline avec refus (RG-S05). Couvre US-S04, US-S05, RG-S04/S05.

**Changements structurants :** `MainViewModel` devient le **seul writer** de `AppConfig` (nouveau ctor
avec `IInputCaptureService` ; méthodes `SetNext/PrevBinding` ; `RaiseConfigChanged` rafraîchit l'onglet
Raccourcis puis notifie l'autosave). `AccountsViewModel.SetDirectBinding` = chemin unique de mutation du
`DirectBinding` (matérialise puis persiste). Encodage `Binding.Code` (souris) élargi — `data-model §Binding`
mis à jour (schemaVersion inchangée). `MainWindow` héberge `ShortcutsView` (onglet Raccourcis câblé).

**Décisions :** [DT-014] capture via abstraction (VM sans WPF) ; [DT-015] seul writer + conflit dans le VM ;
[DT-016] lignes directes sourcées de `AppConfig.Accounts`.

**Vérifications :** `dotnet build` 0 erreur / 0 warning ; `dotnet test` 73/73 (24 nouveaux : encodage
X1/X2 + boutons principaux, formatage, rejet modificateur/combinaison, assignation + émission
`ConfigChanged`, conflit RG-S05 refusé, annulation, effacement, activation directe matérialisée + conflit).

**Dette technique assumée :** logique de la modale WPF (Échap/combinaison en conditions réelles, boutons
souris physiques) non testable unitairement → recette. Onglet Réglages (Axe 7) encore vide. Interception
conditionnée au focus + bascule (US-S01/S02/S03) et rotation `docs/algos/rotation.md` → Axe 6.

### Axe 4 — Ordre & exclusion (onglet Comptes) (2026-09-24)

**Périmètre livré :** réordonnancement des comptes (commandes ↑/↓ sur la sélection + glisser-déposer
via la poignée `☰`) et exclusion/réintégration, avec persistance de l'ordre et du flag `excluded`
(`AccountsViewModel.AccountsChanged` → `MainViewModel` → autosave). Couvre US-D03, US-D04, RG-D04.
Correctif majeur du critère de détection (Axe 3) découvert en recette + fermeture propre du process.

**Changements structurants :** `AccountsViewModel` porte désormais l'ordre persisté mutable + les
commandes ; `MainViewModel.OnAccountsChanged` matérialise l'ordre visible dans `AppConfig.Accounts`
(ordre = rotation, source unique). Reconnaissance DOFUS réécrite pour le client **Unity** réel
(`UnityWndClass` + `Nom - Classe - Version - Type`) — l'ancien motif `" - Dofus"` était erroné.
Ajout d'un journal de diagnostic opt-in (`Diagnostics/DetectionLog`). `ShutdownMode` repassé à
`OnLastWindowClose` en intérim (le tray Axe 7 rétablira l'explicite).

**Décisions :** [DT-010] critère structurel Unity ; [DT-011] journal debug opt-in ; [DT-012]
`ShutdownMode` intérimaire ; [DT-013] matérialisation de l'ordre visible à l'action.

**Vérifications :** `dotnet build` 0 erreur / 0 warning ; `dotnet test` 49/49 (move + bornes, exclusion,
persistance ordre/flag, matérialisation à la 1re action, préservation runtime, DnD, recognizer format
Unity). Recette manuelle : 3 clients détectés/affichés « connecté », réordonnancement + exclusion OK,
croix ferme le process (aucun fantôme). `dotnet publish -r win-x64` single-file OK.

**Dette technique assumée :** onglets Raccourcis (Axe 5) / Réglages (Axe 7) encore vides ; fermeture =
quitter (comportement provisoire jusqu'au tray Axe 7). DnD réordonne mais pas d'indicateur visuel
d'insertion (amélioration UX possible ultérieurement).

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
