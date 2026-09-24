using DofusSwitcher.Models;

namespace DofusSwitcher.ViewModels;

/// <summary>
/// ViewModel racine de la fenêtre principale.
/// Détient la configuration chargée au démarrage et la restitue aux Axes d'édition à venir
/// (Comptes 3-4, Raccourcis 5, Réglages 7). Toute mutation future signalera <see cref="ConfigChanged"/>,
/// que la composition root relaie à l'autosave débouncé — le VM ignore le timer et le disque.
/// </summary>
public sealed class MainViewModel : ObservableObject
{
    /// <summary>Crée le ViewModel racine à partir de la configuration chargée.</summary>
    public MainViewModel(AppConfig config) => Config = config;

    /// <summary>Titre affiché dans la barre de la fenêtre.</summary>
    public string Title => "Dofus Window Switcher";

    /// <summary>Configuration applicative en vigueur (source des onglets d'édition à venir).</summary>
    public AppConfig Config { get; private set; }

    /// <summary>
    /// Émis après toute modification de <see cref="Config"/> ; porte l'instantané à persister.
    /// [DECISION] Aucun émetteur à l'Axe 2 (pas encore d'écran d'édition) : câblage posé pour que
    /// les Axes 4/5/7 déclenchent l'autosave sans retoucher la composition root.
    /// </summary>
    public event Action<AppConfig>? ConfigChanged;

    /// <summary>Remplace la configuration courante et notifie l'autosave. Réservé aux Axes d'édition.</summary>
    private void UpdateConfig(AppConfig config)
    {
        Config = config;
        ConfigChanged?.Invoke(config);
    }
}
