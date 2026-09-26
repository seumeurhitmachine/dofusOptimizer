using DofusSwitcher.Constants;
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
    /// <summary>
    /// Crée le ViewModel racine à partir de la config, du détecteur et des services (capture, registre,
    /// dialogues). Le service de cycle de session et le rappel d'arrêt (Axe 9) sont optionnels : les tests
    /// qui n'exercent pas la session les omettent (l'onglet Comptes désactive alors ses actions process).
    /// </summary>
    public MainViewModel(
        AppConfig config,
        IWindowDetector detector,
        IInputCaptureService capture,
        IStartupRegistryService startup,
        IFileDialogService fileDialog,
        ISessionProcessService? session = null,
        Action? requestShutdown = null,
        Action? requestMinimize = null)
    {
        Config = config;
        // Ouverture de session (Axe 11) : après un « Ouvrir une session » réussi, minimiser l'app SI l'option
        // est active. Le VM Comptes ignore la config : c'est ici (seul lecteur de Config) qu'on décide, puis on
        // délègue la minimisation réelle à la fenêtre (rappel injecté par la composition root).
        Action onSessionOpened = () => { if (Config.MinimizeOnOpenSession) requestMinimize?.Invoke(); };
        // `SetAccountDirectBinding` injecté : le transfert du raccourci direct au lien d'un perso sans compte
        // (Axe 10) écrit côté compte via le seul writer des GameAccounts (MainViewModel).
        Accounts = new AccountsViewModel(config.Accounts, detector, config.GameAccounts, session, requestShutdown,
            SetAccountDirectBinding, onSessionOpened);
        Accounts.SetLauncherPath(config.LauncherPath); // chemin de launcher persisté (Axe 9)
        // Réordonnancement/exclusion/liaison (Axe 4/8) → maj de la config → autosave débouncé (ref [DT-006]).
        Accounts.AccountsChanged += OnAccountsChanged;
        // Raccourcis (Axe 5/8/10) : suivant/précédent top-level ici, activation directe liée au COMPTE (Axe 8,
        // libellé = nom du compte) OU au PERSONNAGE pour les persos sans compte (Axe 10, chemin Accounts.SetDirectBinding).
        Shortcuts = new ShortcutsViewModel(config, capture, SetNextBinding, SetPrevBinding,
            SetAccountDirectBinding, Accounts.SetDirectBinding, Accounts.ConnectedUnlinkedCharacters);
        Accounts.RuntimeChanged += Shortcuts.OnRuntimeChanged; // connexion/déconnexion → libellés directs à jour
        // Réglages (Axe 7/8) : suspension + démarrage Windows + export/import + CRUD comptes, remontés ici (seul writer).
        Settings = new SettingsViewModel(config, startup, fileDialog, SetInterceptionSuspended, SetStartWithWindows,
            SetLauncherPath, SetCloseMinimizes, SetMinimizeToTray, SetMinimizeOnOpenSession, ApplyImportedConfig,
            AddAccount, DeleteAccount, DeleteCharacter, requestShutdown);
    }

    /// <summary>Titre affiché dans la barre de la fenêtre.</summary>
    public string Title => AppConstants.AppTitle;

    /// <summary>ViewModel de l'onglet Comptes : liste temps réel des comptes détectés/persistés.</summary>
    public AccountsViewModel Accounts { get; }

    /// <summary>ViewModel de l'onglet Raccourcis : associations suivant/précédent + directes par compte.</summary>
    public ShortcutsViewModel Shortcuts { get; }

    /// <summary>ViewModel de l'onglet Réglages : suspension, démarrage Windows, export/import, à-propos.</summary>
    public SettingsViewModel Settings { get; }

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

        // Activation directe par COMPTE (Axe 8) → HWND du personnage lié actuellement connecté (0 si aucun).
        var directs = new Dictionary<Binding, nint>();
        foreach (var game in Config.GameAccounts)
        {
            if (game.DirectBinding is null) continue;
            var connected = Accounts.Items.FirstOrDefault(i =>
                i.IsConnected && i.AccountName is not null && NameEquals(i.AccountName, game.Name));
            directs[game.DirectBinding] = connected?.Handle ?? 0;
        }

        // Activation directe par PERSONNAGE pour les persos SANS compte (Axe 10) : portée par
        // AccountConfig.DirectBinding → HWND du personnage connecté (0 sinon). Les persos liés sont ignorés
        // ici (leur activation directe est portée par le compte ; le binding perso est effacé au lien).
        foreach (var character in Config.Accounts)
        {
            if (character.AccountName is not null || character.DirectBinding is null) continue;
            var connected = Accounts.Items.FirstOrDefault(i => i.IsConnected && i.CharacterName == character.CharacterName);
            directs[character.DirectBinding] = connected?.Handle ?? 0;
        }

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

    /// <summary>
    /// Affecte (ou efface) l'entrée d'activation directe d'un <b>compte</b> (Axe 8). Portée par le compte,
    /// pas le personnage : à l'appui, le personnage lié connecté est activé (BuildRotationSnapshot).
    /// </summary>
    private void SetAccountDirectBinding(string accountName, Binding? binding)
    {
        Config = Config with
        {
            GameAccounts = Config.GameAccounts
                .Select(a => NameEquals(a.Name, accountName) ? a with { DirectBinding = binding } : a)
                .ToList(),
        };
        RaiseConfigChanged();
    }

    /// <summary>Bascule la suspension globale de l'interception (RG-T02) — appelée par le tray ou l'onglet Réglages.</summary>
    private void SetInterceptionSuspended(bool suspended)
    {
        Config = Config with { InterceptionSuspended = suspended };
        RaiseConfigChanged();
    }

    /// <summary>Persiste l'intention de démarrage avec Windows (RG-T05). L'écriture registre est faite par le service côté Réglages.</summary>
    private void SetStartWithWindows(bool enabled)
    {
        Config = Config with { StartWithWindows = enabled };
        RaiseConfigChanged();
    }

    /// <summary>
    /// Persiste le chemin de l'Ankama Launcher (Axe 9) et le pousse au VM Comptes (réévalue la résolution et
    /// le gating du bouton « Ouvrir une session »). Chaîne vide → <c>null</c> (efface le chemin configuré).
    /// </summary>
    private void SetLauncherPath(string? path)
    {
        var normalized = string.IsNullOrWhiteSpace(path) ? null : path.Trim();
        Config = Config with { LauncherPath = normalized };
        Accounts.SetLauncherPath(normalized);
        RaiseConfigChanged();
    }

    /// <summary>Persiste le comportement « fermer [X] minimise l'application » (Axe 9). Lu par MainWindow.</summary>
    private void SetCloseMinimizes(bool enabled)
    {
        Config = Config with { CloseMinimizes = enabled };
        RaiseConfigChanged();
    }

    /// <summary>Persiste le comportement « minimiser dans la barre d'état » (Axe 9). Lu par MainWindow.</summary>
    private void SetMinimizeToTray(bool enabled)
    {
        Config = Config with { MinimizeToTray = enabled };
        RaiseConfigChanged();
    }

    /// <summary>Persiste « réduire l'app à l'ouverture d'une session » (Axe 11). Lu par le callback onSessionOpened.</summary>
    private void SetMinimizeOnOpenSession(bool enabled)
    {
        Config = Config with { MinimizeOnOpenSession = enabled };
        RaiseConfigChanged();
    }

    /// <summary>
    /// Applique une configuration importée (US-P04), déjà validée par <see cref="Persistence.ConfigImport"/>.
    /// Remplace la config, recharge les comptes persistés puis rafraîchit tous les onglets et persiste (autosave).
    /// </summary>
    private void ApplyImportedConfig(AppConfig imported)
    {
        Config = imported;
        Accounts.LoadPersisted(imported.Accounts);
        Accounts.LoadGameAccounts(imported.GameAccounts);
        Accounts.SetLauncherPath(imported.LauncherPath);
        RaiseConfigChanged();
    }

    /// <summary>
    /// Crée un compte (Axe 8, US-C01). Valide le nom (RG-C01) et l'unicité insensible à la casse ;
    /// renvoie un message d'erreur si refus, sinon <c>null</c> (créé + persisté). Seul writer.
    /// </summary>
    private string? AddAccount(string name)
    {
        var trimmed = name.Trim();
        if (!GameAccount.IsValidName(trimmed))
            return "Nom invalide : lettres, chiffres, espaces ou tirets (1 à 40 caractères).";
        if (Config.GameAccounts.Any(a => NameEquals(a.Name, trimmed)))
            return "Ce nom de compte existe déjà.";

        Config = Config with { GameAccounts = [.. Config.GameAccounts, new GameAccount(trimmed)] };
        Accounts.LoadGameAccounts(Config.GameAccounts);
        RaiseConfigChanged();
        return null;
    }

    /// <summary>
    /// Supprime un compte (US-C03) : retire le compte ET les personnages liés (cascade, RG-C04), sans
    /// confirmation. Les personnages liés encore connectés réapparaîtront non liés (runtime, zone 2).
    /// </summary>
    private void DeleteAccount(string name)
    {
        if (!Config.GameAccounts.Any(a => NameEquals(a.Name, name))) return;

        var accounts = Config.GameAccounts.Where(a => !NameEquals(a.Name, name)).ToList();
        var characters = Config.Accounts
            .Where(c => c.AccountName is null || !NameEquals(c.AccountName, name))
            .ToList();

        Config = Config with { GameAccounts = accounts, Accounts = characters };
        Accounts.LoadPersisted(Config.Accounts);
        Accounts.LoadGameAccounts(Config.GameAccounts);
        RaiseConfigChanged();
    }

    /// <summary>
    /// Supprime un personnage (sa config persistée) — depuis la liste dépliée d'un compte (Réglages, Axe 8).
    /// S'il est encore connecté, il réapparaît non lié (runtime, zone 2).
    /// </summary>
    private void DeleteCharacter(string characterName)
    {
        if (!Config.Accounts.Any(c => c.CharacterName == characterName)) return;

        var characters = Config.Accounts.Where(c => c.CharacterName != characterName).ToList();
        Config = Config with { Accounts = characters };
        Accounts.LoadPersisted(Config.Accounts);
        RaiseConfigChanged();
    }

    private static bool NameEquals(string a, string b) => string.Equals(a, b, StringComparison.OrdinalIgnoreCase);

    /// <summary>Rafraîchit les onglets Raccourcis et Réglages sur la nouvelle config puis notifie l'autosave.</summary>
    private void RaiseConfigChanged()
    {
        Shortcuts.OnConfigChanged(Config);
        Settings.OnConfigChanged(Config);
        ConfigChanged?.Invoke(Config);
    }
}
