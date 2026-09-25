# Algo — Rotation & décision d'interception

**Dernière mise à jour :** 2026-09-24 — Axe 6 (création)
**Implémentation :** `src/App/Services/SwitchController.cs` (pur), pilotée par `SwitchCoordinator`
**Specs :** `docs/specs/switching.md §2.4/§2.5/§2.6` — CA-01, CA-02, CA-03, C-01, RG-S01..S03/S06

> Décide, à partir d'un instantané de l'état de rotation, d'une entrée physique captée et de la fenêtre
> au premier plan, s'il faut **intercepter** l'entrée et **quelle fenêtre activer**. Fonction pure,
> testable sans interop (`SwitchControllerTests`).

---

## Entrées

- `snapshot` : ordre des comptes (`slots`), associations `next`/`prev`, table `directs` (entrée → HWND).
  - `RotationSlot` = `{ characterName, handle, isConnected, isExcluded }`.
  - **Éligible** (rotation) ssi `isConnected && !isExcluded && handle != 0` (`IsRotatable`).
  - L'ordre des `slots` **est** l'ordre de rotation (data-model §AppConfig).
  - Les `slots` couvrent **tous** les personnages (`AppConfig.Accounts`), qu'ils soient liés à un compte
    ou non : un personnage **sans compte** connecté est un participant de plein droit (Axe 10, [DT-029]).
  - `directs` agrège l'activation directe **par compte** (`GameAccount.DirectBinding` → perso lié connecté,
    [DT-025]) **et par personnage** pour les non liés (`AccountConfig.DirectBinding` → ce perso, Axe 10).
- `input` : entrée captée (`Binding`).
- `foreground` : HWND de la fenêtre au premier plan.

## Sortie

`SwitchDecision { Consume, TargetHandle }` — `Pass` = laisser passer nativement ;
`Handle(h)` = consommer (et activer `h` si `h != 0`).

---

## Décision

```
1. currentIndex ← index du slot connecté dont handle == foreground
   si aucun (le premier plan n'est pas un client DOFUS connu) → Pass        # CA-02, RG-S02
2. si input ∈ directs        → Handle(directs[input])                        # US-S02 (ignore l'exclusion)
3. si input == next          → Handle(step(currentIndex, +1))               # US-S01
4. si input == prev          → Handle(step(currentIndex, -1))
5. sinon (entrée non associée, focus DOFUS) → Pass
```

Le **gate focus DOFUS** (étape 1) précède tout : hors client DOFUS, aucune interception, quel que soit le
binding — le comportement natif est conservé (CA-02). Sur un client DOFUS, une entrée **associée** est
toujours consommée (un seul changement de focus, rien d'autre — C-01/RG-S01), même sans cible.

### step(start, direction)

```
n ← nombre de slots
pour k de 1 à n-1 :                     # k < n : jamais le point de départ (pas de ré-activation de soi)
    i ← ((start + direction*k) mod n + n) mod n
    si slots[i].IsRotatable → retourner slots[i].handle
retourner 0                             # aucun autre compte éligible : consommé, sans activation
```

Cyclique (modulo `n`), **saute les absents et les exclus** (RG-S03). Depuis le dernier présent, `next`
revient au premier présent (CA-01). Un compte absent au milieu est ignoré (CA-03).

---

## Hors périmètre de l'algo (porté par les couches autour)

- **Pré-filtre & suspension** (`SwitchCoordinator`) : entrée non associée ou `interceptionSuspended` →
  `Pass` sans construire d'instantané (hot path, RG-S06).
- **Capture bas niveau** (`InputHook`) : décode l'événement natif → `Binding` (via `InputCapture`).
- **Activation** (`WindowActivator`) : `SetForegroundWindow` + repli `AttachThreadInput` (non
  synthétique, C-03) ; no-op si cible == premier plan.

---

## Historique

| Axe | Date | Modification |
|---|---|---|
| Axe 6 | 2026-09-24 | Création : décision d'interception + rotation cyclique (saut absents/exclus), activation directe. |
| Axe 10 | 2026-09-25 | Précision : les `slots` et `directs` intègrent les personnages **sans compte** (participants de plein droit ; activation directe portée par le personnage). Algorithme inchangé. |
