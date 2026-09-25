using System.Collections.ObjectModel;
using System.IO;
using System.Reflection;
using DofusSwitcher.Constants;
using DofusSwitcher.Models;
using DofusSwitcher.Persistence;
using DofusSwitcher.Services;

namespace DofusSwitcher.ViewModels;

/// <summary>
/// ViewModel de l'onglet Réglages (Axe 7) : suspension globale de l'interception (US-T02), démarrage
/// avec Windows (US-T04, RG-T05), export/import de la configuration (US-P04) et à-propos.
/// [ARCH] Ne référence aucun type WPF : les boîtes de dialogue passent par <see cref="IFileDialogService"/>
/// (ref [DT-014]). N'écrit jamais la config directement — remonte au <see cref="MainViewModel"/> (seul
/// writer, ref [DT-015]) : suspension et démarrage Windows partagent ainsi la même source que le tray.
/// </summary>
public sealed class SettingsViewModel : ObservableObject
{
    private readonly IStartupRegistryService _startup;
    private readonly IFileDialogService _fileDialog;
    private readonly Action<bool> _applySuspended;
    private readonly Action<bool> _applyStartWithWindows;
    private readonly Action<string?> _applyLauncherPath;
    private readonly Action<bool> _applyCloseMinimizes;
    private readonly Action<bool> _applyMinimizeToTray;
    private readonly Action<AppConfig> _applyImported;
    private readonly Func<string, string?> _addAccount;
    private readonly Action<string> _deleteAccount;
    private readonly Action<string> _deleteCharacter;
    private readonly Action? _requestShutdown;
    private System.Windows.Threading.DispatcherTimer? _accountErrorTimer;

    private AppConfig _config;
    private bool _isInterceptionSuspended;
    private bool _startWithWindows;
    private bool _closeMinimizes;
    private bool _minimizeToTray;
    private string? _statusMessage;
    private bool _isStatusError;
    private string _newAccountName = string.Empty;
    private string? _accountError;
    private string _launcherPath = string.Empty;
    private bool _isAddAccountVisible;

    /// <summary>Câble le VM sur la config, les services système et les chemins d'application (MainViewModel).</summary>
    public SettingsViewModel(
        AppConfig config,
        IStartupRegistryService startup,
        IFileDialogService fileDialog,
        Action<bool> applySuspended,
        Action<bool> applyStartWithWindows,
        Action<string?> applyLauncherPath,
        Action<bool> applyCloseMinimizes,
        Action<bool> applyMinimizeToTray,
        Action<AppConfig> applyImported,
        Func<string, string?> addAccount,
        Action<string> deleteAccount,
        Action<string> deleteCharacter,
        Action? requestShutdown = null)
    {
        _config = config;
        _startup = startup;
        _fileDialog = fileDialog;
        _applySuspended = applySuspended;
        _applyStartWithWindows = applyStartWithWindows;
        _applyLauncherPath = applyLauncherPath;
        _applyCloseMinimizes = applyCloseMinimizes;
        _applyMinimizeToTray = applyMinimizeToTray;
        _applyImported = applyImported;
        _addAccount = addAccount;
        _deleteAccount = deleteAccount;
        _deleteCharacter = deleteCharacter;
        _requestShutdown = requestShutdown;

        // [DECISION] État initial lu depuis la config (intention persistée). La réconciliation du registre
        // au chemin de l'exe courant est faite une fois par la composition root au démarrage.
        _isInterceptionSuspended = config.InterceptionSuspended;
        _startWithWindows = config.StartWithWindows;
        _launcherPath = config.LauncherPath ?? string.Empty;
        _closeMinimizes = config.CloseMinimizes;
        _minimizeToTray = config.MinimizeToTray;

        ExportCommand = new RelayCommand(Export);
        ImportCommand = new RelayCommand(Import);
        BrowseLauncherCommand = new RelayCommand(BrowseLauncher);
        CreateAccountCommand = new RelayCommand(CreateAccount);
        ShowAddAccountCommand = new RelayCommand(() => IsAddAccountVisible = true);
        QuitApplicationCommand = new RelayCommand(() => _requestShutdown?.Invoke());
        SupportCommand = new RelayCommand(OpenSupportLink);
        RebuildAccountRows();
    }

    /// <summary>
    /// Suspension globale de l'interception (RG-T02) — partagée avec le tray. Persistée via le MainViewModel ;
    /// lue par le <see cref="SwitchCoordinator"/> à chaque changement de config.
    /// </summary>
    public bool IsInterceptionSuspended
    {
        get => _isInterceptionSuspended;
        set { if (SetProperty(ref _isInterceptionSuspended, value)) _applySuspended(value); }
    }

    /// <summary>Démarrage automatique avec Windows (RG-T05, off par défaut) : pose/retire l'entrée registre et persiste l'intention.</summary>
    public bool StartWithWindows
    {
        get => _startWithWindows;
        set
        {
            if (!SetProperty(ref _startWithWindows, value)) return;
            _startup.SetEnabled(value);       // HKCU\...\Run (sans élévation)
            _applyStartWithWindows(value);    // intention persistée dans la config
        }
    }

    /// <summary>
    /// Cycle de vie : fermer la fenêtre [X] minimise l'application (vrai) ou la quitte (faux). Persisté via
    /// le <see cref="MainViewModel"/> ; lu par <c>MainWindow.OnClosing</c>.
    /// </summary>
    public bool CloseMinimizes
    {
        get => _closeMinimizes;
        set { if (SetProperty(ref _closeMinimizes, value)) _applyCloseMinimizes(value); }
    }

    /// <summary>Cycle de vie : minimiser masque la fenêtre dans la barre d'état plutôt que la barre des tâches.</summary>
    public bool MinimizeToTray
    {
        get => _minimizeToTray;
        set { if (SetProperty(ref _minimizeToTray, value)) _applyMinimizeToTray(value); }
    }

    /// <summary>Visibilité de la ligne de création de compte (dépliée par le bouton « + », repliée en quittant l'onglet).</summary>
    public bool IsAddAccountVisible
    {
        get => _isAddAccountVisible;
        private set => SetProperty(ref _isAddAccountVisible, value);
    }

    /// <summary>Déplie la ligne d'ajout de compte (idempotent : rappuyer quand elle est visible n'a aucun effet).</summary>
    public RelayCommand ShowAddAccountCommand { get; }

    /// <summary>Ferme réellement l'application (bouton Réglages, utile quand [X] minimise au lieu de quitter).</summary>
    public RelayCommand QuitApplicationCommand { get; }

    /// <summary>Replie la ligne d'ajout de compte — appelé quand on quitte l'onglet Réglages (MainWindow).</summary>
    public void CollapseAddAccount() => IsAddAccountVisible = false;

    /// <summary>
    /// Chemin de l'exécutable de l'Ankama Launcher (Axe 9). Vide = auto-détection ; si aucun chemin utilisable,
    /// le bouton « Ouvrir une session » de l'onglet Comptes est masqué. Persisté via le <see cref="MainViewModel"/>.
    /// </summary>
    public string LauncherPath
    {
        get => _launcherPath;
        set { if (SetProperty(ref _launcherPath, value)) _applyLauncherPath(value); }
    }

    /// <summary>Ouvre un sélecteur de fichier pour choisir l'exécutable du launcher.</summary>
    public RelayCommand BrowseLauncherCommand { get; }

    private void BrowseLauncher()
    {
        var path = _fileDialog.AskOpenPath(AppConstants.LauncherFileDialogFilter);
        if (path is not null) LauncherPath = path; // le setter persiste et réévalue le gating
    }

    /// <summary>Texte à-propos : nom de l'application et version.</summary>
    public string AboutText { get; } = $"{AppConstants.AppTitle} · v{ResolveVersion()}";

    /// <summary>Ouvre la page de soutien (Ko-fi) dans le navigateur par défaut.</summary>
    public RelayCommand SupportCommand { get; }

    private static void OpenSupportLink()
    {
        // UseShellExecute : délègue l'ouverture de l'URL au navigateur par défaut (aucune interaction jeu/process cible).
        try
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(AppConstants.SupportUrl)
            {
                UseShellExecute = true,
            });
        }
        catch (Exception ex) when (ex is System.ComponentModel.Win32Exception or InvalidOperationException)
        {
            // Aucun navigateur associé : échec silencieux (le soutien est facultatif).
        }
    }

    /// <summary>Message d'état après export/import (retour visuel discret) ; <c>null</c> si aucun.</summary>
    public string? StatusMessage
    {
        get => _statusMessage;
        private set { if (SetProperty(ref _statusMessage, value)) OnPropertyChanged(nameof(HasStatus)); }
    }

    /// <summary>Vrai s'il y a un message d'état à afficher.</summary>
    public bool HasStatus => !string.IsNullOrEmpty(_statusMessage);

    /// <summary>Vrai si le dernier message d'état est une erreur (pilote la couleur error vs info).</summary>
    public bool IsStatusError
    {
        get => _isStatusError;
        private set => SetProperty(ref _isStatusError, value);
    }

    /// <summary>Exporte la configuration courante vers un fichier choisi (US-P04, RG-P07).</summary>
    public RelayCommand ExportCommand { get; }

    /// <summary>Importe une configuration depuis un fichier, après validation (US-P04, RG-P07).</summary>
    public RelayCommand ImportCommand { get; }

    /// <summary>Comptes déclarés (Axe 8) — lignes avec renommage et suppression (cascade).</summary>
    public ObservableCollection<GameAccountRowViewModel> GameAccounts { get; } = [];

    /// <summary>Vrai si aucun compte n'est déclaré : affiche le texte d'aide sous la section « Compte ».</summary>
    public bool HasNoAccounts => GameAccounts.Count == 0;

    /// <summary>Nom saisi pour créer un nouveau compte (US-C01).</summary>
    public string NewAccountName
    {
        get => _newAccountName;
        set => SetProperty(ref _newAccountName, value);
    }

    /// <summary>Message d'erreur de création de compte (nom invalide ou déjà pris), <c>null</c> si aucun.</summary>
    public string? AccountError
    {
        get => _accountError;
        private set { if (SetProperty(ref _accountError, value)) OnPropertyChanged(nameof(HasAccountError)); }
    }

    /// <summary>Vrai s'il y a une erreur de création de compte à afficher.</summary>
    public bool HasAccountError => !string.IsNullOrEmpty(_accountError);

    /// <summary>Crée un compte à partir de <see cref="NewAccountName"/> (US-C01).</summary>
    public RelayCommand CreateAccountCommand { get; }

    private void CreateAccount()
    {
        var error = _addAccount(NewAccountName);
        SetAccountError(error);
        if (error is null) NewAccountName = string.Empty; // succès : les lignes se rafraîchissent via OnConfigChanged
    }

    /// <summary>
    /// Affecte le message d'erreur de création et arme un effacement automatique après 10 s (recette).
    /// [DECISION] Minuterie UI (auto-dismiss) via <c>DispatcherTimer</c> — seul point où ce VM touche au
    /// timing d'affichage ; créée à la demande (les tests headless ne la font jamais tiquer).
    /// </summary>
    private void SetAccountError(string? error)
    {
        AccountError = error;
        _accountErrorTimer?.Stop();
        if (error is null) return;

        _accountErrorTimer ??= CreateAccountErrorTimer();
        _accountErrorTimer.Start();
    }

    private System.Windows.Threading.DispatcherTimer CreateAccountErrorTimer()
    {
        var timer = new System.Windows.Threading.DispatcherTimer { Interval = TimeSpan.FromSeconds(10) };
        timer.Tick += (_, _) => { timer.Stop(); AccountError = null; };
        return timer;
    }

    /// <summary>Reconstruit les lignes de comptes (nom + personnages liés) depuis la config courante.</summary>
    private void RebuildAccountRows()
    {
        GameAccounts.Clear();
        foreach (var account in _config.GameAccounts)
        {
            var linked = _config.Accounts
                .Where(c => c.AccountName is not null && string.Equals(c.AccountName, account.Name, StringComparison.OrdinalIgnoreCase))
                .Select(c => c.CharacterName)
                .ToList();
            GameAccounts.Add(new GameAccountRowViewModel(account.Name, linked, _deleteAccount, _deleteCharacter));
        }
        OnPropertyChanged(nameof(HasNoAccounts));
    }

    /// <summary>
    /// Réaligne l'affichage sur une nouvelle config (après import ou toute autre persistance). Appelé par le
    /// <see cref="MainViewModel"/>. Ne repasse pas par les setters publics (pas de ré-application/boucle) ;
    /// réconcilie le registre uniquement si <c>StartWithWindows</c> a réellement changé (cas import).
    /// </summary>
    public void OnConfigChanged(AppConfig config)
    {
        _config = config;
        SetProperty(ref _isInterceptionSuspended, config.InterceptionSuspended, nameof(IsInterceptionSuspended));
        if (SetProperty(ref _startWithWindows, config.StartWithWindows, nameof(StartWithWindows)))
            _startup.SetEnabled(config.StartWithWindows);
        SetProperty(ref _launcherPath, config.LauncherPath ?? string.Empty, nameof(LauncherPath));
        SetProperty(ref _closeMinimizes, config.CloseMinimizes, nameof(CloseMinimizes));
        SetProperty(ref _minimizeToTray, config.MinimizeToTray, nameof(MinimizeToTray));
        RebuildAccountRows();
    }

    private void Export()
    {
        var path = _fileDialog.AskSavePath(AppConstants.ConfigFileName);
        if (path is null) return; // annulé

        try
        {
            // Réutilise l'écriture atomique du store standard sur le chemin choisi (RG-P05).
            new JsonConfigStore(path).Save(_config);
            SetStatus("Configuration exportée.", isError: false);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            SetStatus("Échec de l'export : fichier inaccessible.", isError: true);
        }
    }

    private void Import()
    {
        var path = _fileDialog.AskOpenPath(AppConstants.ConfigFileDialogFilter);
        if (path is null) return; // annulé

        string json;
        try
        {
            json = File.ReadAllText(path);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            SetStatus("Échec de l'import : fichier inaccessible.", isError: true);
            return;
        }

        if (ConfigImport.TryParse(json, out var imported, out var error))
        {
            _applyImported(imported!); // → MainViewModel remplace la config, recharge comptes/raccourcis, autosave
            SetStatus("Configuration importée.", isError: false);
        }
        else
        {
            SetStatus(error!, isError: true);
        }
    }

    private void SetStatus(string message, bool isError)
    {
        IsStatusError = isError;
        StatusMessage = message;
    }

    private static string ResolveVersion() =>
        Assembly.GetExecutingAssembly().GetName().Version?.ToString(3) ?? "0.1.0";
}
