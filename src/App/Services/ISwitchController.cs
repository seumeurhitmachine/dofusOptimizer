using DofusSwitcher.Models;

// [WARN] App référence WinForms : Binding est ambigu. Alias explicite.
using Binding = DofusSwitcher.Models.Binding;

namespace DofusSwitcher.Services;

/// <summary>
/// Résultat de la décision d'interception : faut-il consommer l'entrée, et quelle fenêtre activer.
/// </summary>
/// <param name="Consume">Vrai si l'entrée doit être consommée (pas de <c>CallNextHookEx</c>).</param>
/// <param name="TargetHandle">Fenêtre à activer (<c>0</c> = aucune activation, mais entrée éventuellement consommée).</param>
public readonly record struct SwitchDecision(bool Consume, nint TargetHandle)
{
    /// <summary>Laisser passer nativement (comportement natif conservé).</summary>
    public static readonly SwitchDecision Pass = new(false, 0);

    /// <summary>Consommer l'entrée en activant <paramref name="handle"/> (0 = consommée sans activation).</summary>
    public static SwitchDecision Handle(nint handle) => new(true, handle);
}

/// <summary>
/// Décide, à partir d'un <see cref="RotationSnapshot"/>, d'une entrée captée et de la fenêtre au premier
/// plan, s'il faut intercepter et quelle fenêtre activer (rotation cyclique ou activation directe).
/// [ARCH] Pur : aucun type WPF/interop, aucune I/O — testable seul (CA-01/CA-03). L'activation effective
/// et la capture bas niveau sont hors de cette classe.
/// </summary>
public interface ISwitchController
{
    /// <summary>Décide l'issue pour <paramref name="input"/> avec <paramref name="foreground"/> au premier plan.</summary>
    SwitchDecision Decide(RotationSnapshot snapshot, Binding input, nint foreground);
}
