# Dofus Window Switcher — Contexte Projet

**Dernière mise à jour :** 2026-09-24

Outil desktop Windows : détecte les fenêtres des clients DOFUS et bascule le focus
entre eux via des entrées physiques configurables. 100 % local, sans réseau ni
compte. Détail fonctionnel : `docs/specs/_index.md`. Archi normative :
`docs/agent/archis/ARCHI-DOTNET-WPF.md`.

---

## Configuration verrouillée

> Versions figées + mode de gestion. Source unique.

| Composant | Technologie | Version | Géré par |
|---|---|---|---|
| Runtime / SDK | .NET (LTS) | 10.0.x | SDK .NET (`global.json` optionnel) |
| Langage | C# | `LangVersion=latest` | `Directory.Build.props` |
| UI | WPF (`net10.0-windows`) | in-box .NET 10 | `.csproj` (`UseWPF`) |
| Zone de notification | WinForms `NotifyIcon` | in-box .NET 10 | `.csproj` (`UseWindowsForms`) |
| Interop système | P/Invoke Win32 `[LibraryImport]` | in-box | `Interop/NativeMethods.cs` |
| Persistance | `System.Text.Json` (source-gen) | in-box | `Persistence/` |
| Package manager (app) | **Aucun — zéro NuGet** | — | `.csproj` (0 `PackageReference`) |
| Tests (dev-only) | xUnit | 2.x | `tests/App.Tests` (NuGet, hors app) |
| Packaging | Publish single-file self-contained | win-x64 | `.csproj` / `Directory.Build.props` |
| CI/CD | GitHub Actions (`windows-latest`) | — | `.github/workflows/` |
| OS cible | Windows 10/11 x64 | — | contrainte ENF-001 |
| Auth / Secrets / BDD / Caddy / PM2 | **Sans objet** (outil local, pas de réseau) | — | — |

---

## Arborescence

```
dofusOptimizer/
├── docs/{specs,algos,wireframes,agent}/   # specs, ui-design, contexte agent
├── src/App/                               # WPF — Views, ViewModels, Models,
│   ├── Interop/NativeMethods.cs           #   Services, Interop, Persistence,
│   ├── Services/  Persistence/  Tray/     #   Tray, Constants (voir archi §Structure)
│   └── App.xaml(.cs)                       #   composition root
├── tests/App.Tests/                       # xUnit (dev-only)
├── axes/                                   # claude.md archivés
├── claude.md                               # Axe en cours
├── Directory.Build.props · *.sln
```

Structure détaillée et normative : archi §Structure du repo.

---

## Conventions de code

- **Nommage** : idiomatique C# ; suffixes `View`/`ViewModel`/`Service`, interfaces
  `I…`, P/Invoke `internal static partial` (archi §Conventions).
- **Organisation** : **layer-based** — couches à responsabilité unique derrière
  interfaces (Interop · Persistence · Services · ViewModels · Views · Tray).
  Dépendances descendantes uniquement (archi §Découpage en couches).
- **Pattern** : **MVVM manuel léger** (`ObservableObject` + `RelayCommand` maison,
  composition root dans `App.xaml.cs`). Aucun framework MVVM/DI.
- **Constantes** : tunables dans `Constants/AppConstants.cs` ; constantes Win32 au
  plus près de leur P/Invoke ; couleurs/styles dans `ResourceDictionary` XAML.
- **Commentaires** : le « pourquoi ». XML doc sur interfaces de service, méthodes
  publiques de ViewModels, chaque P/Invoke. Préfixes `[ARCH][ALGO][WARN][DECISION]`.

---

## Contraintes structurantes (non négociables)

- **C-02** : jamais de handle sur le *processus* DOFUS, ni mémoire, ni injection,
  ni réseau. Reconnaissance des clients via API *fenêtres* `user32` uniquement.
- **C-03** : aucune entrée synthétique (`SendInput`/`PostMessage`/`SendMessage`),
  même pour forcer le focus.
- **Threading** : callbacks natifs/timers → marshaler via `Dispatcher` avant de
  toucher l'UI ; ne jamais bloquer le thread UI.
- **Publication** : jamais `PublishTrimmed`/`PublishAot` (WPF casse).

---

## Persistance

Config JSON lisible dans `%APPDATA%\DofusSwitcher\config.json` via `IConfigStore`
(source-gen `AppJsonContext`). Écriture **atomique** (temp + move), sauvegarde
**débouncée**, corruption → backup + défaut. Modèle : `docs/data-model.md`.

---

## UI Design

Lire `docs/ui-design.md` avant tout composant UI. Thème sombre, palette gris +
accent teal `#3AAFA9`. **Aucune valeur HEX en dur** hors du `ResourceDictionary`
XAML (`Themes/Colors.xaml`).

---

## Secrets & variables d'environnement

Aucune. Pas d'`.env`, pas de secret, pas de variable de runtime. La seule donnée
persistée (config utilisateur, non sensible) vit dans `%APPDATA%`.

---

## Commandes

```bash
# Dev
dotnet build
dotnet run --project src/App
# Tests
dotnet test tests/App.Tests
# Release (exécutable autonome single-file)
dotnet publish src/App -c Release -r win-x64
```

---

## Fichiers à ne pas modifier sans discussion

- `src/App/Interop/NativeMethods.cs` — seul point de contact Win32 ; toute
  modification touche les contraintes C-02/C-03.
- Propriétés de publication (`Directory.Build.props` / `.csproj`) — packaging
  single-file self-contained figé.
- `src/App/Persistence/AppJsonContext.cs` + schéma `AppConfig` — compat des
  configs existantes (`schemaVersion`).

---

## Références

| Ressource | Chemin |
|---|---|
| Archi normative | `docs/agent/archis/ARCHI-DOTNET-WPF.md` |
| Modèle de données | `docs/data-model.md` |
| État courant projet | `docs/agent/project-state.md` |
| Index specs | `docs/specs/_index.md` |
| UI Design | `docs/ui-design.md` |
| Guide d'init | `docs/init/dotnet-wpf/dotnet-wpf-init.md` |
