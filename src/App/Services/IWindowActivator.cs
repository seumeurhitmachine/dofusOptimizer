namespace DofusSwitcher.Services;

/// <summary>
/// Amène une fenêtre au premier plan sans entrée synthétique (C-03) ni handle de processus (C-02).
/// Seule abstraction d'activation : la logique de décision appelle <see cref="Activate"/>, jamais
/// <c>NativeMethods.SetForegroundWindow</c> directement (archi §Interop).
/// </summary>
public interface IWindowActivator
{
    /// <summary>Handle de la fenêtre actuellement au premier plan.</summary>
    nint GetForeground();

    /// <summary>
    /// Active <paramref name="handle"/> (restaure si réduite, amène au premier plan). No-op si le handle
    /// est nul ou déjà au premier plan.
    /// </summary>
    void Activate(nint handle);
}
