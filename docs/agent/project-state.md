# Project State — Dofus Window Switcher

## État courant

> Mettre à jour à chaque /finalise. Max 30 lignes.

**Dernier Axe complété :** Axe 1 — Scaffolding & socle UI — 2026-09-24
**Phase :** Axes initiaux en cours (Axe 1 livré, Axe 2 à démarrer)

**Fonctionnalités actives :**
- (aucune US — socle infra) Solution `.slnx` + `src/App` (WPF) + `tests/App.Tests` (xUnit).
- Socle MVVM manuel (`ObservableObject`, `RelayCommand`), composition root `App.xaml.cs`
  (`ShutdownMode.OnExplicitShutdown`). Fenêtre à onglets vides (Comptes/Raccourcis/Réglages),
  thème sombre (`Themes/Colors.xaml` + `Controls.xaml`). `Interop/NativeMethods.cs` squelette.
- Publication vérifiée : `App.exe` single-file self-contained win-x64 démarre sans runtime.

**Contraintes techniques actives :**
- .NET 10 + WPF, MVVM manuel, **zéro NuGet** dans l'app, publish single-file self-contained.
- C-02 : aucune interaction avec le processus DOFUS (API fenêtres `user32` seules).
- C-03 : aucune entrée synthétique.
- Threading : callbacks natifs → `Dispatcher` avant toute mutation UI.
- Reconnaissance des clients DOFUS via titre/classe de fenêtre (pas de processus).

**Décisions structurantes en vigueur :**
- [DT-001] Pattern MVVM manuel léger (pas de framework), choisi pour l'UX/évolutions.
- [DT-002] Persistance JSON `%APPDATA%`, écriture atomique + autosave débouncé.
- [DT-003] Lancement manuel, app listée dans les applications Windows ; pas d'auto-démarrage par défaut.
- [DT-004] Namespace racine `DofusSwitcher` (assembly/exe reste `App.exe`) — évite le conflit
  namespace==classe `App` qui casse le point d'entrée WPF généré.
- [DT-005] Props de publication conditionnées à `RuntimeIdentifier != ''` dans `Directory.Build.props` :
  `build`/`test` restent framework-dependent, single-file uniquement au `publish -r win-x64`.

**Fichiers critiques — ne pas modifier sans discussion :**
- `src/App/Interop/NativeMethods.cs` (contraintes C-02/C-03).
- Propriétés de publication (`Directory.Build.props` / `.csproj`).
- `src/App/Persistence/AppJsonContext.cs` + schéma `AppConfig`.

---

## Historique des Axes

<!-- Une entrée par Axe complété. Ajoutée par /finalise. -->

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
