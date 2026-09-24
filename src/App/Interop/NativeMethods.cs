using System.Runtime.InteropServices;

namespace DofusSwitcher.Interop;

/// <summary>
/// Point de contact UNIQUE avec le Win32. Aucune DllImport/LibraryImport ailleurs.
/// [ARCH] Signatures P/Invoke via source-gen [LibraryImport] (classe partial).
/// [WARN] Contraintes C-02 (aucun handle sur le processus DOFUS — API fenêtres user32 seules)
///        et C-03 (aucune entrée synthétique) : toute signature ajoutée ici doit les respecter.
///        En particulier, PAS de GetWindowThreadProcessId/OpenProcess : la reconnaissance des
///        clients passe exclusivement par le titre/la classe de fenêtre (PO-001).
/// [DECISION] EnumWindows et SetWinEventHook prennent un callback. [LibraryImport] ne marshale
///        PAS les délégués (diagnostic SYSLIB1051) : on passe des pointeurs de fonction
///        (delegate* unmanaged&lt;...&gt;), voie compatible source-gen. Les cibles sont des méthodes
///        statiques [UnmanagedCallersOnly] (voir WindowDetector). D'où AllowUnsafeBlocks (App.csproj).
/// Rempli à l'Axe 3 (détection) ; capture (Axe 5) et bascule de focus (Axe 6) l'étendront.
/// Réf : docs/agent/archis/ARCHI-DOTNET-WPF.md §Interop.
/// </summary>
internal static unsafe partial class NativeMethods
{
    // --- Constantes WinEvent (winuser.h) — au plus près de leurs P/Invoke (archi §Constantes). ---

    /// <summary>Une fenêtre/objet a été créé.</summary>
    internal const uint EVENT_OBJECT_CREATE = 0x8000;

    /// <summary>Une fenêtre/objet a été détruit.</summary>
    internal const uint EVENT_OBJECT_DESTROY = 0x8001;

    /// <summary>Le nom (titre) d'un objet a changé — capte le renommage de fenêtre DOFUS.</summary>
    internal const uint EVENT_OBJECT_NAMECHANGE = 0x800C;

    /// <summary>
    /// Le callback est appelé hors contexte : les événements sont postés dans la file de messages
    /// du thread qui a installé le hook (ici le thread UI). Aucune DLL injectée dans le process cible.
    /// </summary>
    internal const uint WINEVENT_OUTOFCONTEXT = 0x0000;

    /// <summary>Ignore les événements produits par le process appelant (bruit inutile).</summary>
    internal const uint WINEVENT_SKIPOWNPROCESS = 0x0002;

    /// <summary>idObject de la fenêtre elle-même (par opposition à un sous-élément).</summary>
    internal const int OBJID_WINDOW = 0;

    /// <summary>idChild de l'objet lui-même (pas un enfant).</summary>
    internal const int CHILDID_SELF = 0;

    // --- Énumération et lecture des fenêtres top-level (user32). ---

    /// <summary>
    /// Énumère toutes les fenêtres top-level en appelant <paramref name="lpEnumFunc"/> pour chacune.
    /// Le callback retourne un non-zéro pour continuer, zéro pour arrêter.
    /// </summary>
    [LibraryImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool EnumWindows(
        delegate* unmanaged[Stdcall]<nint, nint, int> lpEnumFunc,
        nint lParam);

    /// <summary>Indique si une fenêtre est visible (filtre les fenêtres fantômes non affichées).</summary>
    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool IsWindowVisible(nint hWnd);

    /// <summary>Longueur (en caractères, hors terminateur) du titre d'une fenêtre.</summary>
    [LibraryImport("user32.dll", EntryPoint = "GetWindowTextLengthW", SetLastError = true)]
    internal static partial int GetWindowTextLength(nint hWnd);

    /// <summary>
    /// Copie le titre de la fenêtre dans <paramref name="lpString"/> (UTF-16). Retourne le nombre de
    /// caractères copiés. [WARN] Buffer fourni par l'appelant (stackalloc) : pas de StringBuilder,
    /// que [LibraryImport] ne sait pas marshaler.
    /// </summary>
    [LibraryImport("user32.dll", EntryPoint = "GetWindowTextW", SetLastError = true)]
    internal static partial int GetWindowText(nint hWnd, char* lpString, int nMaxCount);

    /// <summary>
    /// Copie le nom de classe de la fenêtre dans <paramref name="lpClassName"/> (UTF-16). Retourne le
    /// nombre de caractères copiés. Utilisé comme signal de reconnaissance (jamais d'inspection process).
    /// </summary>
    [LibraryImport("user32.dll", EntryPoint = "GetClassNameW", SetLastError = true)]
    internal static partial int GetClassName(nint hWnd, char* lpClassName, int nMaxCount);

    // --- Suivi temps réel via WinEvent hook (événementiel, pas de polling — RG-D05). ---

    /// <summary>
    /// Installe un hook d'événements système sur la plage [<paramref name="eventMin"/>,
    /// <paramref name="eventMax"/>]. Avec WINEVENT_OUTOFCONTEXT, <paramref name="lpfnWinEventProc"/>
    /// est appelé sur le thread appelant (doit pomper des messages : le thread UI WPF le fait).
    /// [WARN] Aucun paramètre user-data : le callback statique route vers l'instance via un champ
    /// statique (voir WindowDetector). Retourne un handle à libérer par <see cref="UnhookWinEvent"/>.
    /// </summary>
    [LibraryImport("user32.dll", SetLastError = true)]
    internal static partial nint SetWinEventHook(
        uint eventMin,
        uint eventMax,
        nint hmodWinEventProc,
        delegate* unmanaged[Stdcall]<nint, uint, nint, int, int, uint, uint, void> lpfnWinEventProc,
        uint idProcess,
        uint idThread,
        uint dwFlags);

    /// <summary>Libère un hook installé par <see cref="SetWinEventHook"/>. À appeler à l'arrêt.</summary>
    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool UnhookWinEvent(nint hWinEventHook);
}
