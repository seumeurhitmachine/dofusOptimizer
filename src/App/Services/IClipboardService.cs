namespace DofusSwitcher.Services;

/// <summary>
/// Abstraction du presse-papier système (Axe 13, copie de la formule <c>/invite …</c>). Isole le type WPF
/// <c>System.Windows.Clipboard</c> hors des ViewModels — ceux-ci restent testables avec un faux (ref [DT-014]).
/// </summary>
public interface IClipboardService
{
    /// <summary>
    /// Place <paramref name="text"/> dans le presse-papier. Une chaîne vide efface le presse-papier.
    /// L'implémentation avale les échecs transitoires (presse-papier verrouillé par un autre process).
    /// </summary>
    void SetText(string text);
}
