namespace DofusSwitcher.Services;

/// <summary>
/// Capture globale des entrées physiques par hooks bas niveau (clavier + souris), sans élévation
/// (ENF-005). Chaque appui est remis au gestionnaire fourni à la construction, qui décide s'il faut
/// consommer l'entrée (interception) ou la laisser passer nativement.
/// [ARCH] Seul point d'installation de <c>SetWindowsHookEx</c> côté services ; jamais de hook global
/// ailleurs (la modale de capture de l'Axe 5 n'utilise que les événements de sa fenêtre).
/// </summary>
public interface IInputHook : IDisposable
{
    /// <summary>Installe les hooks bas niveau (sur le thread appelant, qui doit pomper des messages).</summary>
    void Start();

    /// <summary>Libère les hooks.</summary>
    void Stop();
}
