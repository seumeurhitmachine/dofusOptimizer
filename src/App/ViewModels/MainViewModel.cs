using DofusSwitcher.Models;
using DofusSwitcher.Services;

namespace DofusSwitcher.ViewModels;

/// <summary>
/// ViewModel racine de la fenêtre principale.
/// Détient la configuration chargée au démarrage et compose les VM d'onglets (Comptes cet Axe ;
/// Raccourcis 5, Réglages 7 à venir). Toute mutation future signalera <see cref="ConfigChanged"/>,
/// que la composition root relaie à l'autosave débouncé — le VM ignore le timer et le disque.
/// </summary>
public sealed class MainViewModel : ObservableObject
{
    /// <summary>Crée le ViewModel racine à partir de la configuration chargée et du détecteur de fenêtres.</summary>
    public MainViewModel(AppConfig config, IWindowDetector detector)
    {
        Config = config;
        Accounts = new AccountsViewModel(config.Accounts, detector);
        // Réordonnancement/exclusion (Axe 4) → maj de la config → autosave débouncé (ref [DT-006]).
        Accounts.AccountsChanged += OnAccountsChanged;
    }

    /// <summary>Titre affiché dans la barre de la fenêtre.</summary>
    public string Title => "Dofus Window Switcher";

    /// <summary>ViewModel de l'onglet Comptes : liste temps réel des comptes détectés/persistés.</summary>
    public AccountsViewModel Accounts { get; }

    /// <summary>Configuration applicative en vigueur (source des onglets d'édition à venir).</summary>
    public AppConfig Config { get; private set; }

    /// <summary>
    /// Émis après toute modification de <see cref="Config"/> ; porte l'instantané à persister.
    /// Relayé à l'autosave débouncé par la composition root.
    /// </summary>
    public event Action<AppConfig>? ConfigChanged;

    /// <summary>
    /// Intègre le nouvel ordre/état d'exclusion des comptes dans la config et déclenche l'autosave.
    /// L'ordre de la liste <b>est</b> l'ordre de rotation (source unique — data-model §AppConfig).
    /// </summary>
    private void OnAccountsChanged(IReadOnlyList<AccountConfig> accounts)
    {
        Config = Config with { Accounts = [.. accounts] };
        ConfigChanged?.Invoke(Config);
    }
}
