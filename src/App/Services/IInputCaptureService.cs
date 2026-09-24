using DofusSwitcher.Models;

// [WARN] App référence WinForms : Binding est ambigu (System.Windows.Forms.Binding). Alias explicite.
using Binding = DofusSwitcher.Models.Binding;

namespace DofusSwitcher.Services;

/// <summary>
/// Ouvre la modale de capture d'une entrée unique et retourne l'entrée captée.
/// [ARCH] Abstraction qui garde les ViewModels sans type WPF (règle MVVM) et testables via un faux
/// service. L'implémentation (montre <c>CaptureInputWindow</c>) vit côté Views.
/// </summary>
public interface IInputCaptureService
{
    /// <summary>
    /// Affiche la modale libellée par <paramref name="actionLabel"/> et bloque jusqu'à la capture d'une
    /// entrée valide (une seule touche/bouton, combinaisons rejetées) ou l'annulation.
    /// </summary>
    /// <returns>L'entrée captée, ou <c>null</c> si l'utilisateur a annulé (Échap).</returns>
    Binding? Capture(string actionLabel);
}
