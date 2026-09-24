using System.Runtime.InteropServices;

namespace DofusSwitcher.Interop;

/// <summary>
/// Point de contact UNIQUE avec le Win32. Aucune DllImport/LibraryImport ailleurs.
/// [ARCH] Signatures P/Invoke via source-gen [LibraryImport] (classe partial).
/// [WARN] Contraintes C-02 (aucun handle sur le processus DOFUS — API fenêtres user32 seules)
///        et C-03 (aucune entrée synthétique) : toute signature ajoutée ici doit les respecter.
/// Squelette à l'Axe 1 — rempli aux Axes 3 (détection), 5 (capture) et 6 (bascule de focus).
/// Réf : docs/agent/archis/ARCHI-DOTNET-WPF.md §Interop.
/// </summary>
internal static partial class NativeMethods
{
}
