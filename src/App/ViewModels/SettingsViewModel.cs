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
    private readonly Action<AppConfig> _applyImported;

    private AppConfig _config;
    private bool _isInterceptionSuspended;
    private bool _startWithWindows;
    private string? _statusMessage;
    private bool _isStatusError;

    /// <summary>Câble le VM sur la config, les services système et les chemins d'application (MainViewModel).</summary>
    public SettingsViewModel(
        AppConfig config,
        IStartupRegistryService startup,
        IFileDialogService fileDialog,
        Action<bool> applySuspended,
        Action<bool> applyStartWithWindows,
        Action<AppConfig> applyImported)
    {
        _config = config;
        _startup = startup;
        _fileDialog = fileDialog;
        _applySuspended = applySuspended;
        _applyStartWithWindows = applyStartWithWindows;
        _applyImported = applyImported;

        // [DECISION] État initial lu depuis la config (intention persistée). La réconciliation du registre
        // au chemin de l'exe courant est faite une fois par la composition root au démarrage.
        _isInterceptionSuspended = config.InterceptionSuspended;
        _startWithWindows = config.StartWithWindows;

        ExportCommand = new RelayCommand(Export);
        ImportCommand = new RelayCommand(Import);
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

    /// <summary>Texte à-propos : nom de l'application et version.</summary>
    public string AboutText { get; } = $"{AppConstants.AppTitle} · v{ResolveVersion()}";

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
        var path = _fileDialog.AskOpenPath();
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
