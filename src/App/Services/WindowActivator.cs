using DofusSwitcher.Interop;

namespace DofusSwitcher.Services;

/// <summary>
/// Activation de fenêtre via <c>user32</c>. Restaure la fenêtre si réduite puis appelle
/// <c>SetForegroundWindow</c> ; en cas de refus (règles de focus Windows), attache temporairement la
/// file d'entrée du thread courant à celle du premier plan (<c>AttachThreadInput</c>) puis détache.
/// [WARN] <c>AttachThreadInput</c> est une API de <b>gestion de file d'entrée</b>, non synthétique
/// (C-03) ; elle n'utilise que des identifiants de thread — aucun handle de processus (C-02).
/// </summary>
public sealed class WindowActivator : IWindowActivator
{
    /// <inheritdoc/>
    public nint GetForeground() => NativeMethods.GetForegroundWindow();

    /// <inheritdoc/>
    public void Activate(nint handle)
    {
        if (handle == 0) return;

        var foreground = NativeMethods.GetForegroundWindow();
        if (handle == foreground) return; // déjà actif : rien à faire (un seul changement de focus, C-01).

        if (NativeMethods.IsIconic(handle))
            NativeMethods.ShowWindow(handle, NativeMethods.SW_RESTORE);

        if (NativeMethods.SetForegroundWindow(handle))
            return;

        // Refus : attacher la file d'entrée du thread au premier plan à celle du thread cible, forcer le
        // focus, puis détacher. Aucune entrée synthétique n'est émise.
        var currentThread = NativeMethods.GetCurrentThreadId();
        var foregroundThread = NativeMethods.GetWindowThreadProcessId(foreground, 0);
        if (foregroundThread == 0 || foregroundThread == currentThread)
            return;

        if (!NativeMethods.AttachThreadInput(currentThread, foregroundThread, true))
            return;
        try
        {
            NativeMethods.SetForegroundWindow(handle);
        }
        finally
        {
            NativeMethods.AttachThreadInput(currentThread, foregroundThread, false);
        }
    }
}
