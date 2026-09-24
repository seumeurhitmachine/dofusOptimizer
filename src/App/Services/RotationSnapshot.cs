using DofusSwitcher.Models;

// [WARN] App référence WinForms : Binding est ambigu (System.Windows.Forms.Binding). Alias explicite.
using Binding = DofusSwitcher.Models.Binding;

namespace DofusSwitcher.Services;

/// <summary>
/// Un compte dans l'ordre de rotation, avec son état runtime. <see cref="Handle"/> vaut <c>0</c> si absent.
/// Éligible à la rotation cyclique ssi connecté <b>et</b> non exclu (l'activation directe, elle, ignore
/// l'exclusion). Value object pur (pas de type WPF/interop) — foyer testable de la décision.
/// </summary>
/// <param name="CharacterName">Nom de personnage (clé naturelle).</param>
/// <param name="Handle">HWND courant si connecté, sinon <c>0</c>.</param>
/// <param name="IsConnected">Vrai si une fenêtre DOFUS correspondante existe actuellement.</param>
/// <param name="IsExcluded">Vrai si le compte est retiré de la rotation (conservé, EF-09).</param>
public sealed record RotationSlot(string CharacterName, nint Handle, bool IsConnected, bool IsExcluded)
{
    /// <summary>Éligible à la bascule cyclique suivant/précédent (RG-S03).</summary>
    public bool IsRotatable => IsConnected && !IsExcluded && Handle != 0;
}

/// <summary>
/// Instantané immuable de l'état de rotation, capté sur le thread UI au moment d'un appui : ordre des
/// comptes (= ordre de rotation, data-model §AppConfig), associations globales et directes résolues en
/// handles. Consommé par <see cref="SwitchController"/> (pur) pour décider la cible.
/// </summary>
/// <param name="Slots">Comptes dans l'ordre de rotation.</param>
/// <param name="Next">Entrée « suivant » (ou <c>null</c>).</param>
/// <param name="Prev">Entrée « précédent » (ou <c>null</c>).</param>
/// <param name="Directs">Entrées d'activation directe → HWND cible (0 si le compte est absent).</param>
public sealed record RotationSnapshot(
    IReadOnlyList<RotationSlot> Slots,
    Binding? Next,
    Binding? Prev,
    IReadOnlyDictionary<Binding, nint> Directs);
