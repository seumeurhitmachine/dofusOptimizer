# Axe 6 : Interception & bascule de focus (cœur)

refs: docs/plan-axes.md §Axe 6, docs/specs/switching.md §2.4/§2.5/§2.6/§3.x, docs/agent/archis/ARCHI-DOTNET-WPF.md §Interop/§Threading, docs/data-model.md §Binding, src/App/Interop/NativeMethods.cs, src/App/Services/WindowDetector.cs, src/App/Services/DofusWindowRecognizer.cs, src/App/Models/InputCapture.cs, src/App/Models/AppConfig.cs, src/App/Models/AccountConfig.cs, src/App/ViewModels/AccountItemViewModel.cs, src/App/App.xaml.cs
date: 2026-09-24

## tasks
- [ ] P/Invoke hooks bas niveau — `src/App/Interop/NativeMethods.cs` — `SetWindowsHookEx`/`UnhookWindowsHookEx`/`CallNextHookEx` (`WH_KEYBOARD_LL`+`WH_MOUSE_LL`), structs `KBDLLHOOKSTRUCT`/`MSLLHOOKSTRUCT`, `GetForegroundWindow` ; callbacks via pointeurs de fonction `[UnmanagedCallersOnly]` (ref [DT-008])
- [ ] `IInputHook`/`InputHook` — `src/App/Services/` — capture globale ; décode l'événement natif → `Binding` (réutiliser `InputCapture` : `code` souris 1..5) ; callback **minimal** (RG-S06, < 100 ms), aucune I/O
- [ ] Garde focus DOFUS — n'intercepter que si `GetForegroundWindow` est un client DOFUS (`DofusWindowRecognizer`) ; sinon `CallNextHookEx` (RG-S01/S02, C-01, CA-02)
- [ ] Rotation cyclique **pure** — `src/App/Services/` + `docs/algos/rotation.md` (`[ALGO]`) — `ISwitchController` : suivant/précédent, saut des absents/exclus, cyclique (RG-S03, CA-01/CA-03)
- [ ] Activation — `IWindowActivator`/`WindowActivator` — `SetForegroundWindow` + `AttachThreadInput` si refus puis détacher (`[WARN]`, non synthétique C-03) ; jamais de handle process (C-02)
- [ ] Activation directe — appui d'un `directBinding` pendant focus DOFUS → active la fenêtre du compte (US-S02)
- [ ] Suspension — `InterceptionSuspended` vrai ⇒ toujours `CallNextHookEx` (RG-S02 étendu)
- [ ] Câblage — `App.xaml.cs` : composition hook → controller → activator ; source des HWND = comptes détectés (`AccountItemViewModel.Handle`)
- [ ] Tests — `tests/App.Tests/` — rotation pure (CA-01 cyclique, CA-03 saut absents/exclus), décodage entrée→`Binding`, sélection cible

## constraints
- Callback hook = décision + activation synchrone, **jamais d'I/O disque**, latence < 100 ms (RG-S06, ENF-002)
- Activation **non synthétique** : jamais `SendInput`/`PostMessage`/`SendMessage`, même pour forcer le focus (C-03)
- Jamais `OpenProcess`/mémoire/injection sur le process DOFUS — API fenêtres `user32` seules (C-02)
- Hooks `WH_*_LL` **sans élévation** (ENF-005) ; installés/libérés sur le thread UI en pompe de messages
- Rotation dans `docs/algos/rotation.md` ([ALGO]) — après modif algo : MAJ le fichier (working-style)
- ref: project-state.md [DT-008] callbacks natifs via pointeurs de fonction `[UnmanagedCallersOnly]` + routage singleton

## acceptance
Given 4 clients ouverts et un ordre When j'appuie 4× « suivant » Then les 4 comptes sont parcourus cycliquement, focus au 1er (CA-01)
Given un compte absent au milieu de l'ordre When « suivant » Then l'absent est ignoré, le suivant présent reçoit le focus (CA-03)
Given un navigateur au premier plan When bouton « précédent » Then comportement natif conservé, pas d'interception (CA-02, RG-S02)
Given un client DOFUS au premier plan When une entrée associée Then l'entrée est consommée et produit uniquement un changement de focus (C-01, RG-S01)
Given une entrée directe d'un compte When appui pendant focus DOFUS Then la fenêtre de ce compte est activée (US-S02)
Given l'interception suspendue When appui d'une entrée Then transmise nativement (RG-S02)
