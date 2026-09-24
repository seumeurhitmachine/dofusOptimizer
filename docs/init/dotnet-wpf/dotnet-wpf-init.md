# Initialisation — Outil desktop .NET 10 · WPF (zéro NuGet, single-file)

> Guide de setup poste + création de projet pour la stack décrite dans
> `docs/agent/archis/ARCHI-DOTNET-WPF.md`. À lire avant le premier Axe.
> Plateforme de développement : **Windows 10/11 x64** (WPF ne se compile/exécute
> que sous Windows).

---

## 1. Prérequis poste

Seul le **SDK .NET 10** est requis. Rien d'autre (pas de NuGet à gérer, l'app
n'a aucune dépendance externe).

```powershell
# Vérifier une installation existante
dotnet --info

# Installer / mettre à jour via winget (recommandé)
winget install Microsoft.DotNet.SDK.10
```

- Vérifier que `dotnet --version` retourne bien une version `10.x`.
- La charge de travail WPF est incluse dans le SDK Windows par défaut : aucun
  `dotnet workload install` nécessaire.
- IDE au choix : Visual Studio 2022+ (concepteur XAML), VS Code + C# Dev Kit, ou
  Rider. Aucun n'est requis pour builder — `dotnet` en CLI suffit.

---

## 2. Création de la solution

```powershell
# À la racine du projet (déjà bootstrappé par cctemplate)
dotnet new sln -n MonOutil

# Projet WPF principal
dotnet new wpf -n App -o src/App
dotnet sln add src/App/App.csproj

# Projet de tests (dev-only, hors exécutable livré)
dotnet new xunit -n App.Tests -o tests/App.Tests
dotnet sln add tests/App.Tests/App.Tests.csproj
dotnet add tests/App.Tests/App.Tests.csproj reference src/App/App.csproj
```

> L'app livrée reste **zéro NuGet**. Seul `tests/App.Tests` référence xUnit ;
> il ne fait jamais partie de la publication.

---

## 3. Configuration `.csproj` de l'app

Éditer `src/App/App.csproj` pour activer WinForms (tray) en plus de WPF et poser
les propriétés de publication single-file.

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <OutputType>WinExe</OutputType>
    <TargetFramework>net10.0-windows</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <LangVersion>latest</LangVersion>

    <UseWPF>true</UseWPF>
    <UseWindowsForms>true</UseWindowsForms>   <!-- NotifyIcon (tray) in-box -->

    <ApplicationIcon>Resources/app.ico</ApplicationIcon>

    <!-- Publication : exécutable autonome single-file -->
    <RuntimeIdentifier>win-x64</RuntimeIdentifier>
    <SelfContained>true</SelfContained>
    <PublishSingleFile>true</PublishSingleFile>
    <IncludeNativeLibrariesForSelfExtract>true</IncludeNativeLibrariesForSelfExtract>
    <EnableCompressionInSingleFile>true</EnableCompressionInSingleFile>
    <PublishReadyToRun>true</PublishReadyToRun>

    <!-- INTERDIT avec WPF -->
    <PublishTrimmed>false</PublishTrimmed>
  </PropertyGroup>

  <ItemGroup>
    <Resource Include="Resources/app.ico" />   <!-- embarquée pour fenêtre + tray -->
  </ItemGroup>

</Project>
```

Ces propriétés de publication peuvent être factorisées dans un
`Directory.Build.props` à la racine si plusieurs projets les partagent.

---

## 4. Icône

Déposer une icône `Resources/app.ico` (format `.ico` multi-résolution :
16/32/48/256 px). Elle sert à la fois d'icône de l'exécutable (`ApplicationIcon`)
et de ressource embarquée pour la fenêtre et le `NotifyIcon`.

Un `.ico` peut être généré depuis un PNG avec un outil en ligne ou ImageMagick
(`magick convert app.png -define icon:auto-resize=256,48,32,16 app.ico`).

---

## 5. Démarrage manuel (tray)

Pour une app qui vit dans la zone de notification, retirer `StartupUri` de
`App.xaml` et piloter le démarrage dans `App.xaml.cs` (composition root — voir
l'archi §Composition root).

```xml
<!-- App.xaml : PAS de StartupUri -->
<Application x:Class="App.App"
             xmlns="http://schemas.microsoft.com/winfx/2006/xaml/presentation"
             xmlns:x="http://schemas.microsoft.com/winfx/2006/xaml">
  <Application.Resources />
</Application>
```

---

## 6. Boucle de dev

```powershell
dotnet build
dotnet run --project src/App
dotnet test tests/App.Tests
```

---

## 7. Première publication

```powershell
dotnet publish src/App -c Release -r win-x64
```

Récupérer l'exécutable autonome dans :

```
src/App/bin/Release/net10.0-windows/win-x64/publish/App.exe
```

**Valider** en le lançant sur une machine (ou VM) **sans SDK ni runtime .NET
installé** : il doit démarrer sans rien réclamer. C'est le critère de succès du
packaging single-file self-contained.

---

## 8. CI (optionnel)

Sur GitHub Actions, utiliser un runner **`windows-latest`** (WPF n'est pas
buildable sous Linux/macOS) :

```yaml
# .github/workflows/ci.yml (extrait)
runs-on: windows-latest
steps:
  - uses: actions/checkout@v4
  - uses: actions/setup-dotnet@v4
    with: { dotnet-version: '10.0.x' }
  - run: dotnet build --configuration Release
  - run: dotnet test tests/App.Tests
  - run: dotnet publish src/App -c Release -r win-x64   # sur merge main
```
