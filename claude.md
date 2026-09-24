# Axe 1 : Scaffolding & socle UI

refs: docs/plan-axes.md §Axe 1, docs/agent/archis/ARCHI-DOTNET-WPF.md §Structure/§MVVM manuel/§Composition root/§Publication/§Tray, docs/ui-design.md §Palette/§Typographie/§Navigation, docs/init/dotnet-wpf/dotnet-wpf-init.md
date: 2026-09-24

## tasks
- [ ] Solution + projets — `MonOutil.sln`, `src/App` (WPF), `tests/App.Tests` (xUnit) — voir guide d'init
- [ ] `Directory.Build.props` — `net10.0-windows`, single-file self-contained win-x64, `UseWPF`+`UseWindowsForms`, pas de `PublishTrimmed`
- [ ] Vérifier zéro `PackageReference` dans `src/App/App.csproj`
- [ ] Socle MVVM — `src/App/ViewModels/ObservableObject.cs`, `RelayCommand.cs`
- [ ] Composition root — `src/App/App.xaml(.cs)` — `ShutdownMode.OnExplicitShutdown`, `StartupUri` retiré
- [ ] Fenêtre à onglets — `src/App/Views/MainWindow.xaml(.cs)` — shell vide Comptes/Raccourcis/Réglages, code-behind minimal
- [ ] Thème sombre — `src/App/Themes/Colors.xaml` — tokens de `ui-design.md`, référencé dans `App.xaml`
- [ ] Squelette interop — `src/App/Interop/NativeMethods.cs` — `internal static partial`, vide (rempli Axes 3/5/6)
- [ ] Icône — `src/App/Resources/app.ico` — `ApplicationIcon` + `Resource`
- [ ] Test smoke — `tests/App.Tests` — instanciation `MainViewModel`/`RelayCommand`

## constraints
- Axe infra : aucune US livrée ; pas de détection, hook, persistance ni tray (Axes ultérieurs)
- Aucune valeur HEX en dur hors `Themes/Colors.xaml` — ref: project-context.md §UI Design
- ref: project-state.md [DT-001] MVVM manuel (pas de framework MVVM/DI)

## acceptance
Given SDK .NET 10 When dotnet build Then succès sans erreur
Given build When dotnet run --project src/App Then fenêtre thème sombre à 3 onglets vides s'affiche
Given dotnet publish -c Release -r win-x64 When App.exe lancé sur machine sans runtime .NET Then il démarre
Given src/App/App.csproj When inspection Then zéro PackageReference
