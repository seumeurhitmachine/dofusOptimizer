namespace DofusSwitcher.Models;

/// <summary>Nature d'une entrée physique : touche clavier ou bouton souris.</summary>
public enum BindingKind
{
    /// <summary>Touche clavier — <see cref="Binding.Code"/> = Virtual-Key code.</summary>
    Key,

    /// <summary>Bouton souris — <see cref="Binding.Code"/> = index du bouton X (XButton1/2).</summary>
    MouseButton,
}

/// <summary>
/// Une entrée physique unique — une touche <b>ou</b> un bouton souris, sans modificateur (D-01).
/// [ARCH] Value object : pas d'identité propre, sérialisé inline partout (data-model §Binding).
/// Deux <see cref="Binding"/> sont égaux ssi <see cref="Kind"/> et <see cref="Code"/> sont égaux —
/// base de la détection de conflit (unicité globale). L'égalité de valeur est fournie par le record.
/// </summary>
/// <param name="Kind">Touche clavier ou bouton souris.</param>
/// <param name="Code">Virtual-Key code (<see cref="BindingKind.Key"/>) ou index XButton (<see cref="BindingKind.MouseButton"/>).</param>
public sealed record Binding(BindingKind Kind, int Code);
