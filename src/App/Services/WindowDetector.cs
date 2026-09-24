using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Windows.Threading;
using DofusSwitcher.Diagnostics;
using DofusSwitcher.Interop;
using DofusSwitcher.Models;

namespace DofusSwitcher.Services;

/// <summary>
/// Détecteur de fenêtres DOFUS : énumération initiale (<c>EnumWindows</c>) puis suivi temps réel par
/// <c>SetWinEventHook</c> (création/destruction + renommage), sans polling (RG-D05).
/// [ARCH] C-02/PO-001 : reconnaissance via le seul titre/classe de fenêtre (<see cref="DofusWindowRecognizer"/>),
/// jamais d'inspection du processus. [WARN] Les callbacks natifs sont statiques ([UnmanagedCallersOnly],
/// requis par les pointeurs de fonction) et minimaux : ils republient le traitement sur le
/// <see cref="Dispatcher"/> du thread UI, seul autorisé à muter l'état (archi §Threading).
/// [DECISION] SetWinEventHook n'offre aucun paramètre user-data : le callback statique route vers
/// l'unique instance via <see cref="s_current"/>. La composition root n'en crée qu'une (singleton
/// de fait) — invariant qui rend ce champ statique sûr.
/// </summary>
public sealed unsafe class WindowDetector : IWindowDetector
{
    /// <summary>Instance active vers laquelle les callbacks statiques routent (unique, voir [DECISION]).</summary>
    private static WindowDetector? s_current;

    /// <summary>Handles connus comme fenêtres DOFUS → nom de personnage. Muté sur le thread UI uniquement.</summary>
    private readonly Dictionary<nint, string> _known = [];

    private Dispatcher? _dispatcher;
    private nint _createDestroyHook;
    private nint _nameChangeHook;
    private bool _started;

    /// <inheritdoc/>
    public event Action<DetectedWindow>? AccountAppeared;

    /// <inheritdoc/>
    public event Action<DetectedWindow>? AccountDisappeared;

    /// <inheritdoc/>
    public void Start()
    {
        if (_started) return;
        _started = true;
        _dispatcher = Dispatcher.CurrentDispatcher;
        s_current = this;

        DetectionLog.Write("=== Start : énumération initiale ===");

        // Énumération initiale : synchrone sur ce thread (UI) → capte les clients déjà ouverts.
        NativeMethods.EnumWindows(&EnumWindowProc, 0);

        DetectionLog.Write($"=== Énumération terminée : {_known.Count} client(s) DOFUS reconnu(s) ===");

        // Suivi temps réel. Deux plages distinctes plutôt qu'une plage large [CREATE..NAMECHANGE] :
        // évite le bruit des événements intermédiaires (SHOW, HIDE, FOCUS, REORDER…).
        const uint flags = NativeMethods.WINEVENT_OUTOFCONTEXT | NativeMethods.WINEVENT_SKIPOWNPROCESS;
        _createDestroyHook = NativeMethods.SetWinEventHook(
            NativeMethods.EVENT_OBJECT_CREATE, NativeMethods.EVENT_OBJECT_DESTROY,
            hmodWinEventProc: 0, &WinEventProc, idProcess: 0, idThread: 0, flags);
        _nameChangeHook = NativeMethods.SetWinEventHook(
            NativeMethods.EVENT_OBJECT_NAMECHANGE, NativeMethods.EVENT_OBJECT_NAMECHANGE,
            hmodWinEventProc: 0, &WinEventProc, idProcess: 0, idThread: 0, flags);
    }

    /// <inheritdoc/>
    public void Stop()
    {
        if (!_started) return;
        _started = false;

        if (_createDestroyHook != 0) { NativeMethods.UnhookWinEvent(_createDestroyHook); _createDestroyHook = 0; }
        if (_nameChangeHook != 0) { NativeMethods.UnhookWinEvent(_nameChangeHook); _nameChangeHook = 0; }

        _known.Clear();
        if (ReferenceEquals(s_current, this)) s_current = null;
    }

    /// <summary>Libère le hook et l'état natif à l'arrêt de l'application.</summary>
    public void Dispose() => Stop();

    // --- Callbacks natifs (statiques, stdcall) : minimaux, marshalent vers le Dispatcher. ---

    /// <summary>Callback d'<c>EnumWindows</c> : évalue chaque fenêtre visible pendant l'énumération.</summary>
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvStdcall)])]
    private static int EnumWindowProc(nint hwnd, nint lParam)
    {
        try
        {
            var detector = s_current;
            if (detector is null) return 1;

            var visible = NativeMethods.IsWindowVisible(hwnd);
            if (DetectionLog.IsEnabled)
                DetectionLog.Write($"enum hwnd=0x{hwnd:X} visible={(visible ? 1 : 0)} class='{GetClassName(hwnd)}' title='{GetWindowTitle(hwnd)}'");

            if (visible)
                detector.EvaluateWindow(hwnd); // synchrone sur le thread UI (appel depuis Start)
        }
        catch (Exception ex)
        {
            // [WARN] Ne jamais laisser une exception franchir la frontière native (journalisée si debug).
            DetectionLog.Write($"enum EXCEPTION: {ex}");
        }
        return 1; // continuer l'énumération
    }

    /// <summary>Callback du WinEvent hook : capte l'événement puis le republie sur le thread UI.</summary>
    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvStdcall)])]
    private static void WinEventProc(
        nint hWinEventHook, uint eventType, nint hwnd,
        int idObject, int idChild, uint idEventThread, uint dwmsEventTime)
    {
        try
        {
            // Seule la fenêtre elle-même nous intéresse (pas les sous-objets : caret, focus, etc.).
            if (idObject != NativeMethods.OBJID_WINDOW || idChild != NativeMethods.CHILDID_SELF || hwnd == 0)
                return;

            var detector = s_current;
            detector?._dispatcher?.InvokeAsync(() => detector.OnWinEvent(eventType, hwnd));
        }
        catch (Exception ex)
        {
            // [WARN] Ne jamais laisser une exception franchir la frontière native (journalisée si debug).
            DetectionLog.Write($"winevent EXCEPTION: {ex}");
        }
    }

    // --- Traitement, thread UI uniquement. ---

    /// <summary>Traite un événement de fenêtre republié sur le thread UI.</summary>
    private void OnWinEvent(uint eventType, nint hwnd)
    {
        switch (eventType)
        {
            case NativeMethods.EVENT_OBJECT_DESTROY:
                // La fenêtre est déjà partie : impossible de relire son titre → s'appuyer sur le connu.
                if (_known.TryGetValue(hwnd, out var name)) Forget(hwnd, name);
                break;

            case NativeMethods.EVENT_OBJECT_CREATE:
            case NativeMethods.EVENT_OBJECT_NAMECHANGE:
                EvaluateWindow(hwnd);
                break;
        }
    }

    /// <summary>
    /// (Ré)évalue une fenêtre et émet les transitions apparition/disparition qui en découlent.
    /// Gère l'entrée en jeu (login → NAMECHANGE), la sortie et le changement de personnage sur un
    /// même handle. Thread UI uniquement.
    /// </summary>
    private void EvaluateWindow(nint hwnd)
    {
        if (!NativeMethods.IsWindowVisible(hwnd))
        {
            if (_known.TryGetValue(hwnd, out var goneName)) Forget(hwnd, goneName);
            return;
        }

        var title = GetWindowTitle(hwnd);
        var className = GetClassName(hwnd);
        var name = DofusWindowRecognizer.IsDofusWindow(title, className)
            ? DofusWindowRecognizer.ExtractCharacterName(title)
            : null;

        if (DetectionLog.IsEnabled)
            DetectionLog.Write($"eval  hwnd=0x{hwnd:X} class='{className}' title='{title}' → {(name is null ? "REJETÉ" : $"DOFUS «{name}»")}");

        var wasKnown = _known.TryGetValue(hwnd, out var previousName);

        if (name is null)
        {
            // N'est pas (ou plus) un client DOFUS : oublier si on le suivait.
            if (wasKnown) Forget(hwnd, previousName!);
            return;
        }

        if (!wasKnown)
        {
            _known[hwnd] = name;
            AccountAppeared?.Invoke(new DetectedWindow(name, hwnd));
        }
        else if (!string.Equals(previousName, name, StringComparison.Ordinal))
        {
            // Renommage / changement de personnage sur le même handle : disparition puis apparition.
            AccountDisappeared?.Invoke(new DetectedWindow(previousName!, hwnd));
            _known[hwnd] = name;
            AccountAppeared?.Invoke(new DetectedWindow(name, hwnd));
        }
    }

    private void Forget(nint hwnd, string name)
    {
        _known.Remove(hwnd);
        AccountDisappeared?.Invoke(new DetectedWindow(name, hwnd));
    }

    // --- Lecture des chaînes de fenêtre (buffers pile, pas de StringBuilder). ---

    private static string GetWindowTitle(nint hwnd)
    {
        var length = NativeMethods.GetWindowTextLength(hwnd);
        if (length <= 0) return string.Empty;

        var size = length + 1;
        char* buffer = stackalloc char[size];
        var written = NativeMethods.GetWindowText(hwnd, buffer, size);
        return new string(buffer, 0, written);
    }

    private static string GetClassName(nint hwnd)
    {
        const int capacity = 256; // noms de classe Win32 bornés à 256 caractères.
        char* buffer = stackalloc char[capacity];
        var written = NativeMethods.GetClassName(hwnd, buffer, capacity);
        return new string(buffer, 0, written);
    }
}
