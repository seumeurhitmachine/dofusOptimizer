using System.Runtime.InteropServices;
using DofusSwitcher.Services;

// [WARN] UseWPF + UseWindowsForms exposent deux types Clipboard : aliaser vers le presse-papier WPF.
using Clipboard = System.Windows.Clipboard;

namespace DofusSwitcher.Views;

/// <summary>
/// Implémentation WPF de <see cref="IClipboardService"/> via <c>System.Windows.Clipboard</c> (in-box, aucun
/// NuGet). Vit côté Views (seul endroit autorisé à toucher WPF, ref [DT-014]) ; les ViewModels ne dépendent
/// que de l'interface.
/// </summary>
public sealed class ClipboardService : IClipboardService
{
    /// <inheritdoc/>
    public void SetText(string text)
    {
        // [DECISION] Chaîne vide → Clear() : Clipboard.SetText("") lève ArgumentException. [WARN] Le presse-papier
        // peut être temporairement verrouillé par un autre process (COMException/ExternalException) : on avale
        // l'échec (la copie est une commodité, pas une opération critique).
        try
        {
            if (string.IsNullOrEmpty(text)) Clipboard.Clear();
            else Clipboard.SetText(text);
        }
        catch (COMException)
        {
            // Presse-papier indisponible (verrouillé) : échec silencieux.
        }
    }
}
