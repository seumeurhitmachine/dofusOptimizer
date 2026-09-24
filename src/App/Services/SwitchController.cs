using DofusSwitcher.Models;

// [WARN] App référence WinForms : Binding est ambigu. Alias explicite.
using Binding = DofusSwitcher.Models.Binding;

namespace DofusSwitcher.Services;

/// <summary>
/// Implémentation pure de <see cref="ISwitchController"/> — voir <c>docs/algos/rotation.md</c> ([ALGO]).
/// Règles : n'intercepte que si un client DOFUS est au premier plan (RG-S01/S02, CA-02) ; la bascule
/// suivant/précédent est cyclique et saute les comptes absents/exclus (RG-S03, CA-01/CA-03) ;
/// l'activation directe cible le compte associé, connecté, en ignorant l'exclusion (US-S02).
/// </summary>
public sealed class SwitchController : ISwitchController
{
    /// <inheritdoc/>
    public SwitchDecision Decide(RotationSnapshot snapshot, Binding input, nint foreground)
    {
        // Gate focus DOFUS : le premier plan doit être une fenêtre DOFUS connectue (membre du snapshot).
        // Sinon, comportement natif conservé quel que soit le binding (CA-02, RG-S02).
        var currentIndex = IndexOfForeground(snapshot.Slots, foreground);
        if (currentIndex < 0) return SwitchDecision.Pass;

        // Activation directe (ignore l'exclusion) : prioritaire et sans ambiguïté sur l'ordre.
        if (snapshot.Directs.TryGetValue(input, out var directHandle))
            return SwitchDecision.Handle(directHandle);

        // Bascule cyclique suivant/précédent.
        if (snapshot.Next is not null && snapshot.Next == input)
            return SwitchDecision.Handle(StepToRotatable(snapshot.Slots, currentIndex, +1));
        if (snapshot.Prev is not null && snapshot.Prev == input)
            return SwitchDecision.Handle(StepToRotatable(snapshot.Slots, currentIndex, -1));

        // Entrée non associée reçue alors qu'un client DOFUS a le focus : laisser passer.
        return SwitchDecision.Pass;
    }

    /// <summary>Index du compte connecté dont le handle est au premier plan, ou <c>-1</c> si aucun (non-DOFUS).</summary>
    private static int IndexOfForeground(IReadOnlyList<RotationSlot> slots, nint foreground)
    {
        for (var i = 0; i < slots.Count; i++)
            if (slots[i].IsConnected && slots[i].Handle == foreground) return i;
        return -1;
    }

    /// <summary>
    /// Parcourt les comptes en boucle depuis <paramref name="start"/> dans le sens <paramref name="direction"/>
    /// et retourne le premier handle éligible (connecté, non exclu). Retourne <c>0</c> si aucun autre
    /// compte éligible (rotation à vide : entrée consommée, aucune activation).
    /// </summary>
    private static nint StepToRotatable(IReadOnlyList<RotationSlot> slots, int start, int direction)
    {
        // k < n : on visite les autres comptes une fois, jamais le point de départ (pas de ré-activation
        // de soi-même). Aucun autre éligible ⇒ 0 (entrée consommée, sans activation).
        var n = slots.Count;
        for (var k = 1; k < n; k++)
        {
            var i = ((start + direction * k) % n + n) % n;
            if (slots[i].IsRotatable) return slots[i].Handle;
        }
        return 0;
    }
}
