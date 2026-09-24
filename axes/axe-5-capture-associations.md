# Axe 5 : Capture d'entrées & associations

refs: docs/plan-axes.md §Axe 5, docs/specs/switching.md §2.4/§2.5/§3.x, docs/ui-design.md §Composants/§Interactions, docs/wireframes/capture-modal.html, docs/wireframes/raccourcis.html, docs/data-model.md §Binding/§Invariants, docs/agent/archis/ARCHI-DOTNET-WPF.md §MVVM manuel, src/App/Models/Binding.cs, src/App/Models/AppConfig.cs, src/App/Constants/AppConstants.cs, src/App/ViewModels/MainViewModel.cs
date: 2026-09-24

## tasks
- [ ] Modale de capture — `src/App/Views/CaptureInputWindow.xaml(.cs)` — écoute `PreviewKeyDown` + `PreviewMouseDown` de la fenêtre WPF focalisée (**PAS** de hook bas niveau global — Axe 6) ; capte UNE entrée ; Échap = annuler
- [ ] Accepter toute entrée — toute touche (VK) et tout bouton souris **y compris auxiliaires X1/X2** ; rejeter les combinaisons/modificateurs seuls (D-01) — voir mémoire projet
- [ ] Encodage bouton souris → `Binding.Code` — `src/App/Models/Binding.cs` ou helper — cohérent avec défauts `XButton2=2`/`XButton1=1` (`AppConstants`) ; si l'encodage évolue → MAJ `docs/data-model.md §Binding`
- [ ] Formatage lisible d'un `Binding` — helper **pur** `Binding → string` (ex. « XButton2 », « F1 », « A ») pour l'affichage — testable seul
- [ ] `ShortcutsViewModel` — `src/App/ViewModels/` — associations `nextBinding`/`prevBinding` + directes par compte ; bouton « entrée courante » + « Effacer » ; commande d'ouverture de la capture
- [ ] Onglet Raccourcis — `src/App/Views/ShortcutsView.xaml(.cs)` — liste des associations, clic → modale, indicateur de conflit inline (error) sous l'association fautive
- [ ] Détection de conflit — réutiliser l'unicité globale (`AppConfig.HasBindingConflicts`) ; refuser l'enregistrement d'une entrée déjà assignée, message error (RG-S05)
- [ ] Persistance — via `MainViewModel.ConfigChanged` → autosave débouncé (comme Axe 4) ; maj `nextBinding`/`prevBinding`/`directBinding` dans `AppConfig`
- [ ] Câblage — `MainViewModel` expose `ShortcutsViewModel` ; `MainWindow` onglet Raccourcis héberge `ShortcutsView`
- [ ] Tests — `tests/App.Tests/` — rejet combinaison / une seule entrée, conflit (RG-S05), formatage `Binding`, encodage bouton souris, persistance émise

## constraints
- Capture via les événements d'entrée de la fenêtre WPF **focalisée uniquement** — PAS de `SetWindowsHookEx`/`WH_*_LL` (réservé Axe 6) ; minimiser l'interaction (anti-bot)
- **Ne PAS** implémenter l'interception conditionnée au focus ni la bascule de focus (US-S01/S02/S03, Axe 6)
- Une seule entrée par association, aucune combinaison/modificateur (D-01)
- Unicité globale des `Binding` : fichier persisté toujours sans conflit — data-model §Invariants (réutiliser `HasBindingConflicts`)
- Après modif de l'encodage `Binding` : MAJ `docs/data-model.md §Binding` (working-style)
- ref: project-state.md [DT-013] persistance via `ConfigChanged` → autosave

## acceptance
Given l'onglet Raccourcis When je clique « suivant » puis appuie sur un bouton Then l'entrée capturée est assignée et affichée (US-S04)
Given une capture en cours When j'appuie sur un bouton souris auxiliaire (X1/X2) Then il est accepté comme entrée valide
Given une capture en cours When j'appuie sur une combinaison (modificateur+touche) Then elle est rejetée (une seule entrée, D-01)
Given une entrée déjà assignée à une autre action When je tente de l'assigner Then un conflit est signalé et l'enregistrement refusé (US-S05, RG-S05)
Given une capture en cours When j'appuie sur Échap Then la capture est annulée sans modification
