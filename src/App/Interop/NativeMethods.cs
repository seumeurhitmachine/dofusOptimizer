using System.Runtime.InteropServices;

namespace DofusSwitcher.Interop;

/// <summary>
/// Point de contact UNIQUE avec le Win32. Aucune DllImport/LibraryImport ailleurs.
/// [ARCH] Signatures P/Invoke via source-gen [LibraryImport] (classe partial).
/// [WARN] Contraintes C-02 (aucun handle sur le processus DOFUS — API fenêtres user32 seules)
///        et C-03 (aucune entrée synthétique) : toute signature ajoutée ici doit les respecter.
///        Pour la *reconnaissance* des clients : PAS d'inspection process, titre/classe seuls (PO-001).
///        <see cref="GetWindowThreadProcessId"/> n'est utilisé QUE pour l'identifiant de thread requis par
///        <see cref="AttachThreadInput"/> (gestion de file d'entrée, non synthétique) — aucun OpenProcess,
///        aucun handle de processus : conforme C-02/C-03 (switching.md §3.x).
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

    // --- Hooks bas niveau clavier/souris (Axe 6, capture globale sans élévation — ENF-005). ---

    /// <summary>Hook clavier bas niveau (<c>WH_KEYBOARD_LL</c>).</summary>
    internal const int WH_KEYBOARD_LL = 13;

    /// <summary>Hook souris bas niveau (<c>WH_MOUSE_LL</c>).</summary>
    internal const int WH_MOUSE_LL = 14;

    /// <summary>Messages d'appui : touche, touche système (Alt), boutons souris (dont X1/X2).</summary>
    internal const nint WM_KEYDOWN = 0x0100;
    internal const nint WM_SYSKEYDOWN = 0x0104;
    internal const nint WM_LBUTTONDOWN = 0x0201;
    internal const nint WM_RBUTTONDOWN = 0x0204;
    internal const nint WM_MBUTTONDOWN = 0x0207;
    internal const nint WM_XBUTTONDOWN = 0x020B;

    /// <summary>Index de bouton X dans le mot haut de <c>MSLLHOOKSTRUCT.mouseData</c>.</summary>
    internal const int XBUTTON1 = 0x0001;
    internal const int XBUTTON2 = 0x0002;

    /// <summary>Données d'un événement clavier bas niveau (winuser.h).</summary>
    [StructLayout(LayoutKind.Sequential)]
    internal struct KBDLLHOOKSTRUCT
    {
        internal uint vkCode;
        internal uint scanCode;
        internal uint flags;
        internal uint time;
        internal nuint dwExtraInfo;
    }

    /// <summary>Données d'un événement souris bas niveau (winuser.h). <c>mouseData</c> porte l'index XButton.</summary>
    [StructLayout(LayoutKind.Sequential)]
    internal struct MSLLHOOKSTRUCT
    {
        internal int ptX;
        internal int ptY;
        internal uint mouseData;
        internal uint flags;
        internal uint time;
        internal nuint dwExtraInfo;
    }

    /// <summary>
    /// Installe un hook global bas niveau. <paramref name="lpfn"/> est un pointeur de fonction stdcall
    /// (cible <c>[UnmanagedCallersOnly]</c>) : [LibraryImport] ne marshale pas les délégués (SYSLIB1051).
    /// [WARN] Le callback DOIT être court (RG-S06, &lt; 100 ms) sinon Windows le contourne (LowLevelHooksTimeout).
    /// </summary>
    [LibraryImport("user32.dll", EntryPoint = "SetWindowsHookExW", SetLastError = true)]
    internal static partial nint SetWindowsHookEx(
        int idHook,
        delegate* unmanaged[Stdcall]<int, nint, nint, nint> lpfn,
        nint hmod,
        uint dwThreadId);

    /// <summary>Retire un hook installé par <see cref="SetWindowsHookEx"/>.</summary>
    [LibraryImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool UnhookWindowsHookEx(nint hhk);

    /// <summary>Passe l'événement au hook suivant de la chaîne (comportement natif conservé, RG-S02).</summary>
    [LibraryImport("user32.dll")]
    internal static partial nint CallNextHookEx(nint hhk, int nCode, nint wParam, nint lParam);

    /// <summary>Handle de module (base de l'exe si <paramref name="lpModuleName"/> est nul) — requis par le hook LL.</summary>
    [LibraryImport("kernel32.dll", EntryPoint = "GetModuleHandleW", SetLastError = true, StringMarshalling = StringMarshalling.Utf16)]
    internal static partial nint GetModuleHandle(string? lpModuleName);

    // --- Activation de fenêtre (Axe 6). Non synthétique (C-03), aucun handle process (C-02). ---

    /// <summary>Handle de la fenêtre au premier plan.</summary>
    [LibraryImport("user32.dll")]
    internal static partial nint GetForegroundWindow();

    /// <summary>
    /// Amène une fenêtre au premier plan. [WARN] La valeur de retour n'est PAS fiable : Windows peut
    /// renvoyer vrai tout en refusant le vol de focus (verrou de premier plan) et se contenter de faire
    /// clignoter la fenêtre — d'où l'attache de file d'entrée systématique dans <c>WindowActivator</c>.
    /// </summary>
    [LibraryImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool SetForegroundWindow(nint hWnd);

    /// <summary>Place une fenêtre en tête de l'ordre Z (complète <see cref="SetForegroundWindow"/>).</summary>
    [LibraryImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool BringWindowToTop(nint hWnd);

    /// <summary>Vrai si la fenêtre est réduite (icône) — pour la restaurer avant activation.</summary>
    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool IsIconic(nint hWnd);

    /// <summary>Change l'état d'affichage d'une fenêtre (<see cref="SW_RESTORE"/> pour dé-réduire).</summary>
    [LibraryImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool ShowWindow(nint hWnd, int nCmdShow);

    /// <summary>Restaure une fenêtre réduite/agrandie à sa taille précédente.</summary>
    internal const int SW_RESTORE = 9;

    /// <summary>
    /// Identifiant du thread propriétaire de la fenêtre (le <paramref name="lpdwProcessId"/> est ignoré, 0).
    /// [WARN] Utilisé UNIQUEMENT pour <see cref="AttachThreadInput"/> : identifiant de thread, jamais de
    /// handle de processus (C-02). Ne pas détourner pour inspecter le process.
    /// </summary>
    [LibraryImport("user32.dll", SetLastError = true)]
    internal static partial uint GetWindowThreadProcessId(nint hWnd, nint lpdwProcessId);

    /// <summary>
    /// Surcharge renvoyant l'<b>identifiant de processus</b> propriétaire de la fenêtre (Axe 9).
    /// [ARCH] Exception C-02 assumée (ref [DT-027]) : le PID sert au <b>cycle de session</b>
    /// (fermeture d'un client via <c>SessionProcessService.KillByHandle</c>), jamais à inspecter la
    /// mémoire/le titre du jeu. On lit le PID depuis la fenêtre — <b>aucun</b> <c>OpenProcess</c> ici ;
    /// l'ouverture d'un handle process a lieu, côté service, uniquement pour le kill (System.Diagnostics).
    /// </summary>
    [LibraryImport("user32.dll", SetLastError = true)]
    internal static partial uint GetWindowThreadProcessId(nint hWnd, out uint lpdwProcessId);

    /// <summary>PID du processus propriétaire de <paramref name="hWnd"/> (0 si la fenêtre est invalide).</summary>
    internal static uint GetProcessIdFromWindow(nint hWnd)
    {
        _ = GetWindowThreadProcessId(hWnd, out uint pid);
        return pid;
    }

    /// <summary>Identifiant du thread courant (pour l'attache de file d'entrée).</summary>
    [LibraryImport("kernel32.dll")]
    internal static partial uint GetCurrentThreadId();

    /// <summary>
    /// Attache/détache la file d'entrée de deux threads (<c>fAttach</c>). API de gestion de file d'entrée,
    /// non synthétique (C-03) : contourne le refus de <see cref="SetForegroundWindow"/> sans injecter d'entrée.
    /// </summary>
    [LibraryImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    internal static partial bool AttachThreadInput(uint idAttach, uint idAttachTo, [MarshalAs(UnmanagedType.Bool)] bool fAttach);
}
