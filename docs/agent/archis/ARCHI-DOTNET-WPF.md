---
stack: .NET 10 + WPF + MVVM manuel + P/Invoke Win32 + System.Text.Json (zéro NuGet)
aliases: [wpf, dotnet-wpf, dotnet-desktop, csharp-desktop, winapp]
category: desktop
language: csharp
---

<!-- INSTRUCTIONS AGENT
Ce fichier est une RÉFÉRENCE D'ARCHITECTURE fixe pour la stack
.NET 10 (LTS) + WPF + MVVM manuel léger + interop Win32 (P/Invoke) +
System.Text.Json, publiée en exécutable autonome single-file, ZÉRO package NuGet
dans l'application livrée.

RÈGLES :
- NE JAMAIS modifier ce fichier.
- Il est lu par l'agent lors de la génération de docs/agent/project-context.md
  pour tout projet utilisant cette stack.
- Il ne remplace pas project-context.md — il l'alimente.
- Les conventions ici sont NON NÉGOCIABLES sauf décision explicite
  documentée dans la spec principale de docs/specs/ (§stack).
-->

# Architecture de référence — Outil desktop Windows · .NET 10 · WPF · MVVM manuel

**Version :** 1.0
**Stack :** .NET 10 (LTS) · WPF · MVVM manuel léger · P/Invoke Win32 (`[LibraryImport]`) · System.Text.Json (source-gen) · NotifyIcon (WinForms in-box) · publish single-file self-contained · **zéro package NuGet dans l'app**
**Guide d'initialisation :** `docs/init/dotnet-wpf/dotnet-wpf-init.md`

---

## Principe directeur

Cette archi vise l'**outil desktop Windows léger** : petite UI de configuration,
logique système via P/Invoke, persistance locale en JSON, présence dans la zone
de notification. Le contrat structurant :

- **Zéro dépendance NuGet dans l'application livrée.** Tout est dans le SDK .NET :
  WPF, WinForms (pour `NotifyIcon`), `System.Text.Json`, interop Win32. Aucun
  `PackageReference` dans le `.csproj` de l'app. (Un projet de tests peut, lui,
  référencer un framework de test — voir §Tests.)
- **Publié en un seul exécutable autonome** (`self-contained` + `PublishSingleFile`),
  ne requérant aucun runtime .NET installé sur la machine cible.
- **Côté développement, seul le SDK .NET 10 est requis.**
- **MVVM manuel léger** : `INotifyPropertyChanged` fait main, `ICommand` maison,
  composition root explicite dans `App.xaml.cs`. Pas de framework MVVM, pas de
  conteneur DI.

---

## Initialisation poste et projet

Avant le premier setup, lire :

- `docs/init/dotnet-wpf/dotnet-wpf-init.md`

Ce guide couvre l'installation du SDK .NET 10, la création de la solution, les
propriétés `.csproj` de publication single-file, l'icône embarquée et la première
publication. Il complète cette archi ; il ne remplace pas les conventions de repo
et de code ci-dessous.

---

## Stack verrouillée

| Composant | Technologie | Contrainte |
|---|---|---|
| Runtime | .NET 10 (LTS) | TFM `net10.0-windows` |
| UI | WPF (`UseWPF=true`) | XAML + MVVM manuel |
| Zone de notification | `System.Windows.Forms.NotifyIcon` (`UseWindowsForms=true`) | In-box, pas de NuGet |
| Interop système | P/Invoke via `[LibraryImport]` (source-gen) | Regroupé dans `Interop/NativeMethods.cs` |
| Persistance | `System.Text.Json` + `JsonSerializerContext` (source-gen) | Fichier local dans `%APPDATA%` |
| Pattern UI | MVVM manuel (`ObservableObject`, `RelayCommand` maison) | Aucun framework MVVM |
| DI | Composition root manuelle dans `App.xaml.cs` | Aucun conteneur |
| Packaging | `dotnet publish` single-file self-contained | `win-x64` (+ `win-arm64` si requis) |
| Package manager | **Aucun** dans l'app (zéro `PackageReference`) | Tests exclus |
| CI/CD | GitHub + GitHub Actions (runner `windows-latest`) | |

**Non supporté avec cette stack :** WPF n'est **pas** compatible AOT natif ni
trimming agressif. Ne jamais activer `PublishTrimmed` ni `PublishAot` sur le
projet WPF — le rendu XAML casse. Le single-file self-contained non-trimmé est la
cible de publication.

---

## Structure du repo

```
MonOutil/
├── src/
│   └── App/                                # Projet WPF principal — net10.0-windows
│       ├── App.xaml                        # Ressources globales + StartupUri absent (démarrage manuel)
│       ├── App.xaml.cs                     # COMPOSITION ROOT — instancie services, VM, fenêtre, tray
│       │
│       ├── Views/                          # XAML + code-behind MINIMAL (InitializeComponent + wiring)
│       │   ├── MainWindow.xaml
│       │   ├── MainWindow.xaml.cs
│       │   └── [Nom]View.xaml(.cs)
│       │
│       ├── ViewModels/                     # Logique d'écran — classes C# pures, zéro référence WPF
│       │   ├── ObservableObject.cs         # Base INotifyPropertyChanged (maison)
│       │   ├── RelayCommand.cs             # ICommand (maison) — sync + générique
│       │   ├── MainViewModel.cs
│       │   └── [Nom]ViewModel.cs
│       │
│       ├── Models/                         # Types de données — records + config sérialisable
│       │   ├── AppConfig.cs                # Racine sérialisée en JSON
│       │   └── [Domain].cs
│       │
│       ├── Services/                       # UNE couche = UNE interface + UNE implémentation
│       │   ├── I[X]Service.cs
│       │   └── [X]Service.cs               # Détection, capture d'entrées, activation, etc.
│       │
│       ├── Interop/
│       │   └── NativeMethods.cs            # TOUT le P/Invoke Win32 — seul point de contact natif
│       │
│       ├── Tray/
│       │   └── TrayIconController.cs       # Wrapper NotifyIcon (WinForms) + menu contextuel
│       │
│       ├── Persistence/
│       │   ├── IConfigStore.cs
│       │   ├── JsonConfigStore.cs          # Charge/sauve AppConfig — écriture atomique
│       │   └── AppJsonContext.cs           # JsonSerializerContext source-gen
│       │
│       ├── Constants/
│       │   └── AppConstants.cs             # Chemins, timeouts, valeurs par défaut
│       │
│       ├── Resources/
│       │   └── app.ico                     # Icône embarquée (fenêtre + tray)
│       │
│       └── App.csproj
│
├── tests/
│   └── App.Tests/                          # xUnit (dev-only) — ViewModels + services testables
│       └── App.Tests.csproj
│
├── docs/
│   ├── specs/                              # docs/specs/[domaine].md + _index.md
│   ├── data-model.md
│   ├── algos/
│   ├── ui-design.md
│   ├── wireframes/
│   └── agent/
│       ├── project-context.md
│       └── archis/ARCHI-DOTNET-WPF.md
│
├── axes/
├── claude.md
├── Directory.Build.props                   # LangVersion, Nullable, TFM, props de publication communs
├── .editorconfig
└── MonOutil.sln
```

---

## Conventions — Fichiers et nommage

| Élément | Convention | Exemple |
|---|---|---|
| Vues | PascalCase + suffixe `View`/`Window` | `MainWindow.xaml` |
| ViewModels | PascalCase + suffixe `ViewModel` | `MainViewModel.cs` |
| Services (interfaces) | `I` + PascalCase + `Service` | `IWindowActivator.cs` |
| Services (implémentations) | PascalCase (sans `I`) | `WindowActivator.cs` |
| Modèles / config | PascalCase, `record` de préférence | `AppConfig.cs` |
| Constantes (fichiers) | PascalCase + `Constants` | `AppConstants.cs` |
| Constantes (valeurs) | `PascalCase` (const C#) ou `SCREAMING_SNAKE` si emprunt Win32 | `DefaultPollIntervalMs` / `WM_HOTKEY` |
| P/Invoke | `internal static partial` dans `NativeMethods` | `GetForegroundWindow` |
| Branches Git | `axe-N-titre-court` | `axe-2-input-capture` |

---

## MVVM manuel — le socle (2 classes maison)

Zéro NuGet ⇒ pas de `CommunityToolkit.Mvvm`. Les deux briques ci-dessous suffisent
et sont écrites **une seule fois** à l'Axe 1.

```csharp
// ViewModels/ObservableObject.cs
using System.ComponentModel;
using System.Runtime.CompilerServices;

/// <summary>Base de tous les ViewModels : notification de changement de propriété.</summary>
public abstract class ObservableObject : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>Affecte <paramref name="field"/> et notifie si la valeur change. Retourne true si modifié.</summary>
    protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? name = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value)) return false;
        field = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        return true;
    }

    protected void OnPropertyChanged([CallerMemberName] string? name = null)
        => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
```

```csharp
// ViewModels/RelayCommand.cs
using System.Windows.Input;

/// <summary>ICommand synchrone maison. Pour l'async, encapsuler l'appel et gérer l'état soi-même.</summary>
public sealed class RelayCommand(Action execute, Func<bool>? canExecute = null) : ICommand
{
    public event EventHandler? CanExecuteChanged;
    public bool CanExecute(object? _) => canExecute?.Invoke() ?? true;
    public void Execute(object? _) => execute();
    public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
}
```

Règles MVVM :
- **Les ViewModels ne référencent JAMAIS de type WPF** (`Window`, `Control`,
  `Visibility`, `Brush`…). Ils exposent des `bool`, `string`, `enum`,
  `ObservableCollection<T>`. La conversion vers l'UI se fait par binding/converters.
- **Le code-behind d'une View est minimal** : `InitializeComponent()`, câblage du
  cycle de vie fenêtre (fermeture → cacher dans le tray), et rien d'autre. Aucune
  règle métier dans un `.xaml.cs`.
- Les collections liées à l'UI sont des `ObservableCollection<T>` mutées **sur le
  thread UI** uniquement (voir §Threading).

---

## Composition root — `App.xaml.cs`

Toutes les instances sont créées **une fois**, dans l'ordre de dépendance, au
démarrage. Pas de conteneur DI : le graphe est petit et explicite.

```csharp
// App.xaml.cs — StartupUri retiré du App.xaml ; démarrage piloté ici.
public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // 1. Persistance (aucune dépendance)
        IConfigStore configStore = new JsonConfigStore();
        var config = configStore.Load();

        // 2. Services système (dépendent éventuellement de la config)
        //    Ex. : détection, capture d'entrées, activation de fenêtre.
        var someService = new SomeService(/* deps */);

        // 3. ViewModel racine (dépend des services + de la config)
        var mainVm = new MainViewModel(someService, configStore, config);

        // 4. Fenêtre principale
        var window = new MainWindow { DataContext = mainVm };

        // 5. Tray (garde une référence forte — sinon NotifyIcon disparaît au GC)
        _tray = new TrayIconController(window, mainVm);

        // ShutdownMode manuel : fermer la fenêtre ne quitte pas l'app (elle vit dans le tray).
        ShutdownMode = ShutdownMode.OnExplicitShutdown;
        window.Show();
    }

    private TrayIconController? _tray;

    protected override void OnExit(ExitEventArgs e)
    {
        _tray?.Dispose(); // libère l'icône du tray, sinon elle reste "fantôme" jusqu'au survol
        base.OnExit(e);
    }
}
```

Ordre imposé : **persistance → services → ViewModel → View → tray**. Un service
qui manque une dépendance est une erreur de bootstrap (échec au démarrage), pas
une erreur runtime.

---

## Découpage en couches (obligatoire)

Le code applicatif est découpé en couches à responsabilité unique, chacune
derrière une interface, injectée par la composition root. C'est la garantie de
testabilité et le pendant WPF de la règle « couches séparées ».

| Couche | Rôle | Dépendances autorisées |
|---|---|---|
| `Interop/` | P/Invoke brut, `SafeHandle`, structs Win32 | Aucune (feuille) |
| `Persistence/` | Charger/sauver la config en JSON | `Models/`, `Constants/` |
| `Services/` | Logique système (via `Interop`) exposée en interfaces C# propres | `Interop/`, `Models/` |
| `ViewModels/` | Logique d'écran, orchestration des services | `Services/`, `Persistence/`, `Models/` |
| `Views/` | XAML + binding | `ViewModels/` (par `DataContext`) |
| `Tray/` | Présence zone de notification, menu, show/hide fenêtre | `ViewModels/`, `Views/` |

Règle de dépendance : **les flèches ne remontent jamais**. Un `Service` ne connaît
pas un `ViewModel` ; un `ViewModel` ne connaît pas une `View`. La communication
montante se fait par événement C# ou callback, jamais par référence directe.

---

## Interop Win32 — `Interop/NativeMethods.cs`

**Tout** le P/Invoke est regroupé dans ce seul fichier. Aucune `DllImport` /
`LibraryImport` ailleurs. Utiliser le **source generator** `[LibraryImport]`
(.NET 7+) plutôt que `[DllImport]` : plus rapide, sans marshaling runtime, et le
compilateur valide les signatures.

```csharp
// Interop/NativeMethods.cs
using System.Runtime.InteropServices;

/// <summary>
/// Point de contact unique avec le Win32. Signatures P/Invoke via source-gen.
/// La classe est partial car [LibraryImport] génère l'implémentation.
/// </summary>
internal static partial class NativeMethods
{
    [LibraryImport("user32.dll")]
    internal static partial IntPtr GetForegroundWindow();

    [LibraryImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool SetForegroundWindow(IntPtr hWnd);

    // Les strings passent en UTF-16 : préciser StringMarshalling.
    [LibraryImport("user32.dll", StringMarshalling = StringMarshalling.Utf16)]
    internal static partial int GetWindowTextLength(IntPtr hWnd);
}
```

Règles interop :
- Le fichier contenant du `LibraryImport` doit être `partial` (source-gen).
- Encapsuler chaque appel natif brut derrière une **méthode C# de service** au nom
  métier : le reste du code appelle `IWindowActivator.Activate(handle)`, jamais
  `NativeMethods.SetForegroundWindow` directement.
- Libérer les handles natifs via `SafeHandle` ou `try/finally` — jamais fuiter.
- Toujours `SetLastError = true` sur les API qui documentent `GetLastError`, et
  lire l'erreur via `Marshal.GetLastPInvokeError()`.
- Documenter, en commentaire `[WARN]`, toute contrainte système non évidente
  (permissions, UIPI/élévation, threads, restrictions de `SetForegroundWindow`).

---

## Persistance — `System.Text.Json` source-gen

Config sérialisée dans `%APPDATA%\<AppName>\config.json`. Le source generator
(`JsonSerializerContext`) est obligatoire : rapide, prévisible, compatible
publication (pas de réflexion dynamique).

```csharp
// Persistence/AppJsonContext.cs
using System.Text.Json.Serialization;

[JsonSourceGenerationOptions(WriteIndented = true, PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(AppConfig))]
internal partial class AppJsonContext : JsonSerializerContext { }
```

```csharp
// Persistence/JsonConfigStore.cs
public sealed class JsonConfigStore : IConfigStore
{
    private static readonly string Path = System.IO.Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        AppConstants.AppFolderName, "config.json");

    public AppConfig Load()
    {
        try
        {
            if (!File.Exists(Path)) return AppConfig.Default;
            var json = File.ReadAllText(Path);
            return JsonSerializer.Deserialize(json, AppJsonContext.Default.AppConfig)
                   ?? AppConfig.Default;
        }
        catch (Exception ex) when (ex is JsonException or IOException)
        {
            // Config illisible : conserver une copie fautive puis repartir vide.
            TryBackupCorrupt();
            return AppConfig.Default;
        }
    }

    public void Save(AppConfig config)
    {
        Directory.CreateDirectory(System.IO.Path.GetDirectoryName(Path)!);
        // Écriture atomique : temp + remplacement, pour ne jamais laisser un fichier tronqué.
        var tmp = Path + ".tmp";
        File.WriteAllText(tmp, JsonSerializer.Serialize(config, AppJsonContext.Default.AppConfig));
        File.Move(tmp, Path, overwrite: true);
    }

    private void TryBackupCorrupt() { /* copie Path → Path + ".corrupt-<timestamp>" en best-effort */ }
}
```

Règles persistance :
- **Écriture atomique** systématique (temp + `File.Move` overwrite) — jamais
  d'écriture en place qui puisse laisser un fichier à moitié écrit.
- Un fichier de config corrompu ne fait jamais planter l'app : sauvegarde de la
  copie fautive + config par défaut.
- Sauvegarde **débouncée** si les modifications sont fréquentes (ne pas écrire le
  disque à chaque frappe) — voir §Threading.

---

## Zone de notification — `NotifyIcon` (WinForms in-box)

WPF n'a pas d'icône de tray native. La voie **zéro-NuGet** est
`System.Windows.Forms.NotifyIcon`, disponible dans le SDK desktop en activant
`UseWindowsForms` **en plus** de `UseWPF`.

```xml
<!-- App.csproj -->
<UseWPF>true</UseWPF>
<UseWindowsForms>true</UseWindowsForms>
```

```csharp
// Tray/TrayIconController.cs
using WinForms = System.Windows.Forms;

/// <summary>Icône de zone de notification + menu. Garder une référence forte (App).</summary>
public sealed class TrayIconController : IDisposable
{
    private readonly WinForms.NotifyIcon _icon;

    public TrayIconController(Window window, MainViewModel vm)
    {
        _icon = new WinForms.NotifyIcon
        {
            Icon = new System.Drawing.Icon(
                Application.GetResourceStream(
                    new Uri("Resources/app.ico", UriKind.Relative))!.Stream),
            Visible = true,
            Text = AppConstants.AppTitle,
        };
        _icon.DoubleClick += (_, _) => ShowWindow(window);
        _icon.ContextMenuStrip = BuildMenu(window, vm);
    }

    private static void ShowWindow(Window w)
    {
        w.Show();
        w.WindowState = WindowState.Normal;
        w.Activate();
    }

    public void Dispose()
    {
        _icon.Visible = false; // sinon l'icône reste fantôme jusqu'au survol
        _icon.Dispose();
    }

    private WinForms.ContextMenuStrip BuildMenu(Window w, MainViewModel vm) { /* ... */ }
}
```

Règles tray :
- `App` garde une **référence forte** au controller ; sinon le `NotifyIcon` est
  collecté et disparaît.
- Toujours `Dispose()` dans `OnExit` (icône fantôme sinon).
- Fermer la fenêtre = la cacher (`e.Cancel = true; Hide();` dans `Closing`), pas
  quitter. Le vrai `Shutdown()` passe par le menu tray.

---

## Threading — la règle qui casse tout si oubliée

WPF est **single-thread apartment (STA)** : tout accès à un contrôle ou à une
`ObservableCollection` liée doit se faire sur le thread UI. Les callbacks système
(hooks Win32, timers, tâches de fond) arrivent souvent sur un **autre thread**.

```csharp
// Depuis un callback hors thread UI : marshaler vers le Dispatcher.
Application.Current.Dispatcher.Invoke(() => _items.Add(newItem));
// ou BeginInvoke pour ne pas bloquer le thread appelant.
```

Règles threading :
- Muter toute donnée liée à l'UI uniquement via `Dispatcher.Invoke/BeginInvoke`.
- Ne jamais bloquer le thread UI (`.Result`, `.Wait()`, `Thread.Sleep`) — l'UI
  gèle. Utiliser `async/await` avec un `DispatcherTimer` pour le polling.
- Un hook/callback natif doit être **le plus court possible** : capter la donnée,
  la republier sur le Dispatcher, revenir immédiatement.
- Débouncer les sauvegardes disque (`DispatcherTimer` de courte durée réarmé à
  chaque modification) plutôt qu'écrire à chaque événement.

---

## Publication — exécutable autonome single-file

Cible : **un seul `.exe`**, sans runtime .NET préinstallé.

```xml
<!-- Directory.Build.props (extraits communs) -->
<PropertyGroup>
  <TargetFramework>net10.0-windows</TargetFramework>
  <Nullable>enable</Nullable>
  <LangVersion>latest</LangVersion>
  <ImplicitUsings>enable</ImplicitUsings>
  <ApplicationIcon>Resources/app.ico</ApplicationIcon>

  <!-- Publication single-file self-contained -->
  <RuntimeIdentifier>win-x64</RuntimeIdentifier>
  <SelfContained>true</SelfContained>
  <PublishSingleFile>true</PublishSingleFile>
  <IncludeNativeLibrariesForSelfExtract>true</IncludeNativeLibrariesForSelfExtract>
  <EnableCompressionInSingleFile>true</EnableCompressionInSingleFile>

  <!-- INTERDIT avec WPF : casse le rendu XAML -->
  <PublishTrimmed>false</PublishTrimmed>
  <PublishReadyToRun>true</PublishReadyToRun>
</PropertyGroup>
```

```bash
dotnet publish src/App -c Release -r win-x64
# → bin/Release/net10.0-windows/win-x64/publish/App.exe (autonome)
```

Règles publication :
- **Jamais** `PublishTrimmed` ni `PublishAot` (WPF incompatible).
- `ReadyToRun` accéléré le démarrage à froid ; c'est le seul « AOT partiel » permis.
- L'icône est référencée par `ApplicationIcon` (exe) **et** embarquée en ressource
  (`Resources/app.ico`, `Resource` build action) pour la fenêtre et le tray.
- Élévation : ne pas exiger l'admin par défaut. Si l'outil doit interagir avec des
  processus élevés, le documenter — ne pas embarquer un manifeste `requireAdministrator`
  sans nécessité.

---

## Tests

L'app livrée est zéro-NuGet ; le **projet de tests** est un projet séparé
(`tests/App.Tests`) et peut, lui, référencer un framework de test (xUnit). Il ne
fait pas partie de l'exécutable publié.

- Cibler les **ViewModels** (logique d'écran) et les **Services** (logique système
  derrière interface, avec `NativeMethods` mocké via l'interface du service).
- Les Views et l'interop Win32 brut ne sont pas testés unitairement (dépendance
  système / UI) — les couvrir par la recette manuelle.
- Si le « zéro NuGet » doit s'étendre aux tests, écrire un harnais d'assertions
  console minimal ; par défaut, xUnit sur le projet de tests est accepté.

```bash
dotnet test tests/App.Tests
```

---

## Conventions — Commentaires

Commenter le **pourquoi**, pas le **quoi**. XML doc (`/// <summary>`) obligatoire
sur : interfaces de service, méthodes publiques des ViewModels, `NativeMethods`
(chaque P/Invoke : ce que fait l'API et ses pièges), modèles sérialisés.

Préfixes orientés agent dans le code :

| Préfixe | Usage |
|---|---|
| `[ARCH]` | Contrainte architecturale — ne pas contourner (ex. « tout P/Invoke ici ») |
| `[ALGO]` | Pointer vers `docs/algos/<nom>.md` |
| `[WARN]` | Piège non évident (thread UI, handle natif, restriction Win32) |
| `[DECISION]` | Arbitrage d'implémentation qui pourrait surprendre |

---

## Secrets, réseau, configuration distante

Par défaut, un outil desktop local n'a **ni secret ni backend** : rien à chiffrer,
pas d'`.env`, pas d'Infisical. La config utilisateur (non sensible) vit en clair
dans `%APPDATA%`.

Si l'outil acquiert plus tard un backend (licence en ligne, sync, télémétrie) :
- Router les appels via un unique `HttpClient` singleton (jamais instancié par appel).
- Sortir les secrets de build/CI dans **Infisical** ; ne jamais les coder en dur ni
  les committer. Documenter alors les variables dans project-context §Config verrouillée.

---

## Hooks Claude Code (settings.partial.json)

> Fusionné dans `.claude/settings.json` par le bootstrap quand cette archi est sélectionnée.
> Contient uniquement les hooks spécifiques à la stack (.NET / WPF).

```json
{
  "hooks": {
    "PostToolUse": [
      {
        "matcher": "Write(**/*.cs)",
        "hooks": [
          { "type": "command", "command": "cd $(git rev-parse --show-toplevel) && dotnet build --nologo --verbosity quiet 2>&1 | tail -20" }
        ]
      },
      {
        "matcher": "Write(**/*.xaml)",
        "hooks": [
          { "type": "command", "command": "cd $(git rev-parse --show-toplevel) && dotnet build --nologo --verbosity quiet 2>&1 | tail -20" }
        ]
      }
    ]
  }
}
```

---

## Commandes de développement

```bash
# Dev
dotnet restore
dotnet build
dotnet run --project src/App

# Tests
dotnet test tests/App.Tests

# Release / publish (exécutable autonome single-file)
dotnet publish src/App -c Release -r win-x64
# Variante ARM64 si besoin :
dotnet publish src/App -c Release -r win-arm64
```

---

## Règles de constantes spécifiques (stack C#/WPF)

- Tous les tunables (timeouts, intervalles de polling, tailles par défaut, seuils)
  dans `Constants/AppConstants.cs` — jamais de magic number dans les services/VM.
- Toutes les constantes Win32 empruntées (`WM_*`, `HWND_*`, flags) dans
  `Interop/NativeMethods.cs`, au plus près de leurs P/Invoke.
- Chemins de fichiers (dossier `%APPDATA%`, nom du fichier de config) centralisés
  dans `AppConstants` — jamais reconstruits ailleurs.
- Couleurs, tailles et styles de l'UI dans les `ResourceDictionary` XAML
  (`App.xaml` ou un `Themes/*.xaml`), pas en dur dans le code-behind.
- Textes visibles (titres, libellés du menu tray) centralisés — prévoir une
  `Strings` statique si l'app doit être localisable.

---

## Points d'attention récurrents

**Cycle de vie**
- `ShutdownMode.OnExplicitShutdown` quand l'app vit dans le tray : sinon fermer la
  dernière fenêtre quitte l'app.
- `NotifyIcon` : référence forte + `Dispose` obligatoire (icône fantôme sinon).

**Interop**
- Regrouper 100 % du P/Invoke dans `NativeMethods` ; encapsuler derrière un service.
- `SetLastError = true` + `Marshal.GetLastPInvokeError()` sur les API concernées.
- Libérer tout handle natif (`SafeHandle`/`try-finally`).

**Threading**
- Callbacks natifs/timers → marshaler vers `Dispatcher` avant de toucher l'UI.
- Ne jamais bloquer le thread UI ; débouncer les I/O disque.

**Publication**
- Jamais `PublishTrimmed`/`PublishAot` (WPF casse). `ReadyToRun` OK.
- Vérifier que l'`.exe` publié démarre sur une machine **sans** SDK/runtime .NET.

**Persistance**
- Écriture atomique ; config corrompue → backup + défaut, jamais de crash.
