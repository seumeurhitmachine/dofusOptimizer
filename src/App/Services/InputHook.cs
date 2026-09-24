using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using DofusSwitcher.Interop;
using DofusSwitcher.Models;

// [WARN] App référence WinForms : Binding est ambigu. Alias explicite.
using Binding = DofusSwitcher.Models.Binding;

namespace DofusSwitcher.Services;

/// <summary>
/// Hooks bas niveau <c>WH_KEYBOARD_LL</c> + <c>WH_MOUSE_LL</c> : décode chaque appui en <see cref="Binding"/>
/// (réutilise <see cref="InputCapture"/>, encodage cohérent avec la capture Axe 5) et interroge le
/// gestionnaire ; <c>true</c> ⇒ entrée consommée (<c>return 1</c>), sinon <c>CallNextHookEx</c>.
/// [WARN] Le callback s'exécute sur le thread installateur (thread UI en pompe de messages) et doit
/// rester bref (RG-S06, &lt; 100 ms) : décode + délègue, aucune I/O. Ne filtre que les messages d'appui
/// (ignore mouvements/relâchements) pour limiter le coût sur le flot d'événements souris.
/// [DECISION] Callbacks statiques <c>[UnmanagedCallersOnly]</c> (pointeurs de fonction, cf. [DT-008]) :
/// routage vers l'unique instance via <see cref="s_current"/> (une seule créée par la composition root).
/// </summary>
public sealed unsafe class InputHook : IInputHook
{
    private static InputHook? s_current;

    private readonly Func<Binding, bool> _onInput;
    private nint _keyboardHook;
    private nint _mouseHook;
    private bool _started;

    /// <summary>Crée le hook. <paramref name="onInput"/> retourne <c>true</c> pour consommer l'entrée.</summary>
    public InputHook(Func<Binding, bool> onInput) => _onInput = onInput;

    /// <inheritdoc/>
    public void Start()
    {
        if (_started) return;
        _started = true;
        s_current = this;

        var hmod = NativeMethods.GetModuleHandle(null);
        _keyboardHook = NativeMethods.SetWindowsHookEx(NativeMethods.WH_KEYBOARD_LL, &KeyboardProc, hmod, 0);
        _mouseHook = NativeMethods.SetWindowsHookEx(NativeMethods.WH_MOUSE_LL, &MouseProc, hmod, 0);
    }

    /// <inheritdoc/>
    public void Stop()
    {
        if (!_started) return;
        _started = false;

        if (_keyboardHook != 0) { NativeMethods.UnhookWindowsHookEx(_keyboardHook); _keyboardHook = 0; }
        if (_mouseHook != 0) { NativeMethods.UnhookWindowsHookEx(_mouseHook); _mouseHook = 0; }

        if (ReferenceEquals(s_current, this)) s_current = null;
    }

    /// <summary>Libère les hooks à l'arrêt de l'application.</summary>
    public void Dispose() => Stop();

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvStdcall)])]
    private static nint KeyboardProc(int nCode, nint wParam, nint lParam)
    {
        try
        {
            var self = s_current;
            if (self is not null && nCode == 0 &&
                (wParam == NativeMethods.WM_KEYDOWN || wParam == NativeMethods.WM_SYSKEYDOWN))
            {
                var data = *(NativeMethods.KBDLLHOOKSTRUCT*)lParam;
                if (self._onInput(InputCapture.FromKey((int)data.vkCode)))
                    return 1; // consommée
            }
        }
        catch
        {
            // [WARN] Ne jamais laisser une exception franchir la frontière native : laisser passer.
        }
        return NativeMethods.CallNextHookEx(0, nCode, wParam, lParam);
    }

    [UnmanagedCallersOnly(CallConvs = [typeof(CallConvStdcall)])]
    private static nint MouseProc(int nCode, nint wParam, nint lParam)
    {
        try
        {
            var self = s_current;
            if (self is not null && nCode == 0)
            {
                var button = ToButton(wParam, lParam);
                if (button is not null && self._onInput(InputCapture.ToBinding(button.Value)))
                    return 1; // consommée
            }
        }
        catch
        {
            // [WARN] Ne jamais laisser une exception franchir la frontière native : laisser passer.
        }
        return NativeMethods.CallNextHookEx(0, nCode, wParam, lParam);
    }

    /// <summary>Traduit un message souris d'appui en bouton capté ; <c>null</c> pour tout autre message.</summary>
    private static CapturedMouseButton? ToButton(nint message, nint lParam)
    {
        if (message == NativeMethods.WM_LBUTTONDOWN) return CapturedMouseButton.Left;
        if (message == NativeMethods.WM_RBUTTONDOWN) return CapturedMouseButton.Right;
        if (message == NativeMethods.WM_MBUTTONDOWN) return CapturedMouseButton.Middle;
        if (message == NativeMethods.WM_XBUTTONDOWN)
        {
            var data = *(NativeMethods.MSLLHOOKSTRUCT*)lParam;
            var xButton = (int)((data.mouseData >> 16) & 0xFFFF); // mot haut = index XButton
            return xButton switch
            {
                NativeMethods.XBUTTON1 => CapturedMouseButton.XButton1,
                NativeMethods.XBUTTON2 => CapturedMouseButton.XButton2,
                _ => null,
            };
        }
        return null;
    }
}
