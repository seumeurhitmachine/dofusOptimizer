using DofusSwitcher.Models;
using DofusSwitcher.Services;

// [WARN] App référence WinForms : Binding est ambigu (System.Windows.Forms.Binding). Alias explicite.
using Binding = DofusSwitcher.Models.Binding;

namespace DofusSwitcher.ViewModels;

/// <summary>
/// ViewModel racine de la fenêtre principale.
/// Détient la configuration chargée au démarrage et compose les VM d'onglets (Comptes, Raccourcis ;
/// Réglages 7 à venir). Il est le <b>seul writer</b> de <see cref="Config"/> : les VM enfants remontent
/// leurs intentions, il les applique et émet <see cref="ConfigChanged"/>, que la composition root relaie
/// à l'autosave débouncé — le VM ignore le timer et le disque.
/// </summary>
public sealed class MainViewModel : ObservableObject
{
    /// <summary>Crée le ViewModel racine à partir de la config, du détecteur et du service de capture.</summary>
    public MainViewModel(AppConfig config, IWindowDetector detector, IInputCaptureService capture)
    {
        Config = config;
        Accounts = new AccountsViewModel(config.Accounts, detector);
        // Réordonnancement/exclusion (Axe 4) → maj de la config → autosave débouncé (ref [DT-006]).
        Accounts.AccountsChanged += OnAccountsChanged;
        // Raccourcis (Axe 5) : suivant/précédent top-level ici, activation directe via AccountsViewModel
        // (chemin unique de mutation des comptes — préserve le DirectBinding lors d'un réordonnancement).
        Shortcuts = new ShortcutsViewModel(config, capture, SetNextBinding, SetPrevBinding, Accounts.SetDirectBinding);
    }

    /// <summary>Titre affiché dans la barre de la fenêtre.</summary>
    public string Title => "Dofus Window Switcher";

    /// <summary>ViewModel de l'onglet Comptes : liste temps réel des comptes détectés/persistés.</summary>
    public AccountsViewModel Accounts { get; }

    /// <summary>ViewModel de l'onglet Raccourcis : associations suivant/précédent + directes par compte.</summary>
    public ShortcutsViewModel Shortcuts { get; }

    /// <summary>
    /// Construit l'instantané de rotation (Axe 6) depuis la config (ordre, associations) et l'état runtime
    /// des comptes (handles/connecté/exclu via <see cref="AccountsViewModel.Items"/>). [WARN] À appeler sur
    /// le thread UI uniquement (lit la collection d'items) — c'est le cas depuis le callback du hook.
    /// </summary>
    public RotationSnapshot BuildRotationSnapshot()
    {
        // Ordre = ordre d'affichage des comptes (= ordre de rotation, source unique). Handle/état viennent
        // du runtime ; l'exclusion est celle affichée (reflète la config matérialisée).
        var slots = Accounts.Items
            .Select(i => new RotationSlot(i.CharacterName, i.Handle, i.IsConnected, i.IsExcluded))
            .ToList();

        // Associations directes → HWND résolu par nom (0 si le compte n'est pas connecté).
        var handlesByName = slots
            .Where(s => s.IsConnected)
            .ToDictionary(s => s.CharacterName, s => s.Handle, StringComparer.Ordinal);
        var directs = new Dictionary<Binding, nint>();
        foreach (var account in Config.Accounts)
            if (account.DirectBinding is not null)
                directs[account.DirectBinding] = handlesByName.GetValueOrDefault(account.CharacterName);

        return new RotationSnapshot(slots, Config.NextBinding, Config.PrevBinding, directs);
    }

    /// <summary>Configuration applicative en vigueur (source des onglets d'édition à venir).</summary>
    public AppConfig Config { get; private set; }

    /// <summary>
    /// Émis après toute modification de <see cref="Config"/> ; porte l'instantané à persister.
    /// Relayé à l'autosave débouncé par la composition root.
    /// </summary>
    public event Action<AppConfig>? ConfigChanged;

    /// <summary>
    /// Intègre le nouvel ordre/état d'exclusion (et les activations directes) des comptes dans la config
    /// et déclenche l'autosave. L'ordre de la liste <b>est</b> l'ordre de rotation (data-model §AppConfig).
    /// </summary>
    private void OnAccountsChanged(IReadOnlyList<AccountConfig> accounts)
    {
        Config = Config with { Accounts = [.. accounts] };
        RaiseConfigChanged();
    }

    /// <summary>Applique l'entrée « suivant » (ou l'efface si <c>null</c>). Top-level, sans conflit de recouvrement.</summary>
    private void SetNextBinding(Binding? binding)
    {
        Config = Config with { NextBinding = binding };
        RaiseConfigChanged();
    }

    /// <summary>Applique l'entrée « précédent » (ou l'efface si <c>null</c>).</summary>
    private void SetPrevBinding(Binding? binding)
    {
        Config = Config with { PrevBinding = binding };
        RaiseConfigChanged();
    }

    /// <summary>Rafraîchit l'onglet Raccourcis sur la nouvelle config puis notifie l'autosave.</summary>
    private void RaiseConfigChanged()
    {
        Shortcuts.OnConfigChanged(Config);
        ConfigChanged?.Invoke(Config);
    }
}
