using DofusSwitcher.Interop;

namespace DofusSwitcher.Services;

/// <summary>
/// Activation de fenêtre via <c>user32</c>. Restaure la fenêtre si réduite, puis force le premier plan
/// en attachant la file d'entrée du thread courant à celle du thread au premier plan
/// (<c>AttachThreadInput</c>) le temps de l'appel — seul moyen fiable de contourner le verrou de premier
/// plan de Windows sans entrée synthétique.
/// [WARN] On n'utilise JAMAIS la valeur de retour de <c>SetForegroundWindow</c> comme succès : Windows
/// peut renvoyer vrai sans activer (simple clignotement) → l'attache est faite systématiquement, sinon
/// il faut deux appuis pour basculer.
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

        // Attacher la file d'entrée du thread courant à celle du premier plan : leur état d'entrée partagé
        // autorise SetForegroundWindow à activer réellement la cible (contourne le verrou, sans synthétique).
        var currentThread = NativeMethods.GetCurrentThreadId();
        var foregroundThread = NativeMethods.GetWindowThreadProcessId(foreground, 0);
        var attached = foregroundThread != 0
            && foregroundThread != currentThread
            && NativeMethods.AttachThreadInput(currentThread, foregroundThread, true);
        try
        {
            NativeMethods.BringWindowToTop(handle);
            NativeMethods.SetForegroundWindow(handle);
        }
        finally
        {
            if (attached) NativeMethods.AttachThreadInput(currentThread, foregroundThread, false);
        }
    }
}
