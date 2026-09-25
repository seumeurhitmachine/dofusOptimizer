using System.ComponentModel;
using System.Windows;
using DofusSwitcher.Constants;
using DofusSwitcher.ViewModels;

using WinForms = System.Windows.Forms;
using Drawing = System.Drawing;
// [WARN] UseWPF + UseWindowsForms exposent deux Application/Window/MessageBox. Lever l'ambiguïté vers WPF.
using Application = System.Windows.Application;

namespace DofusSwitcher.Tray;

/// <summary>
/// Présence dans la zone de notification (US-T01/T02) : icône <see cref="WinForms.NotifyIcon"/> (WinForms
/// in-box) + menu contextuel (Ouvrir · Suspendre/Réactiver · Quitter). Double-clic → affiche la fenêtre.
/// [ARCH] Couche Tray : dépend de la View (fenêtre) et du ViewModel, jamais l'inverse. La suspension est
/// basculée via <see cref="SettingsViewModel.IsInterceptionSuspended"/> — même source que l'onglet Réglages
/// (aucun couplage UI→hook). L'état suspendu est reflété par l'info-bulle et la case du menu (ref décision
/// Axe 7 : pas d'icône grisée distincte, évite la gestion d'un HICON GDI runtime, RG-T06).
/// [WARN] <see cref="App"/> garde une référence forte à ce contrôleur : sans elle, le GC collecte le
/// <see cref="WinForms.NotifyIcon"/> et l'icône disparaît.
/// </summary>
public sealed class TrayIconController : IDisposable
{
    private const string OpenLabel = "Ouvrir";
    private const string SuspendLabel = "Suspendre l'interception";
    private const string QuitLabel = "Quitter";

    private readonly Window _window;
    private readonly SettingsViewModel _settings;
    private readonly WinForms.NotifyIcon _icon;
    private readonly WinForms.ToolStripMenuItem _suspendItem;
    private readonly Drawing.Icon? _trayIcon;

    /// <summary>Crée l'icône du tray et son menu, câblés sur la fenêtre et les réglages.</summary>
    public TrayIconController(Window window, MainViewModel mainViewModel)
    {
        _window = window;
        _settings = mainViewModel.Settings;

        _trayIcon = LoadTrayIcon();
        _suspendItem = new WinForms.ToolStripMenuItem(SuspendLabel, image: null, (_, _) => ToggleSuspended())
        {
            CheckOnClick = false, // l'état vient de la config (source unique), pas du clic
        };

        _icon = new WinForms.NotifyIcon
        {
            Icon = _trayIcon,
            Visible = true,
            Text = AppConstants.AppTitle,
            ContextMenuStrip = BuildMenu(),
        };
        _icon.DoubleClick += (_, _) => ShowWindow();

        _settings.PropertyChanged += OnSettingsPropertyChanged;
        UpdateSuspendedVisual(); // reflète l'état persisté au démarrage
    }

    private WinForms.ContextMenuStrip BuildMenu()
    {
        var menu = new WinForms.ContextMenuStrip();
        menu.Items.Add(new WinForms.ToolStripMenuItem(OpenLabel, image: null, (_, _) => ShowWindow()));
        menu.Items.Add(new WinForms.ToolStripSeparator());
        menu.Items.Add(_suspendItem);
        menu.Items.Add(new WinForms.ToolStripSeparator());
        menu.Items.Add(new WinForms.ToolStripMenuItem(QuitLabel, image: null, (_, _) => Quit()));
        return menu;
    }

    /// <summary>Charge l'icône embarquée (Resources/app.ico) pour le tray ; best-effort (null si absente).</summary>
    private static Drawing.Icon? LoadTrayIcon()
    {
        var stream = Application.GetResourceStream(new Uri("Resources/app.ico", UriKind.Relative))?.Stream;
        return stream is null ? null : new Drawing.Icon(stream);
    }

    private void OnSettingsPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(SettingsViewModel.IsInterceptionSuspended))
            UpdateSuspendedVisual();
    }

    private void ToggleSuspended() => _settings.IsInterceptionSuspended = !_settings.IsInterceptionSuspended;

    /// <summary>Reflète l'état suspendu : case cochée du menu + info-bulle de l'icône (RG-T02).</summary>
    private void UpdateSuspendedVisual()
    {
        var suspended = _settings.IsInterceptionSuspended;
        _suspendItem.Checked = suspended;
        _icon.Text = suspended ? $"{AppConstants.AppTitle} — suspendu" : $"{AppConstants.AppTitle} — actif";
    }

    private void ShowWindow()
    {
        _window.Show();
        _window.WindowState = WindowState.Normal;
        _window.Activate();
    }

    /// <summary>Sortie réelle de l'application (RG-T01) : autorise la fermeture puis déclenche l'arrêt explicite.</summary>
    private void Quit()
    {
        if (_window is Views.MainWindow main) main.ForceClose = true;
        Application.Current.Shutdown();
    }

    /// <summary>Libère l'icône (sinon fantôme jusqu'au survol, RG-T06) et se désabonne.</summary>
    public void Dispose()
    {
        _settings.PropertyChanged -= OnSettingsPropertyChanged;
        _icon.Visible = false;
        _icon.Dispose();
        _trayIcon?.Dispose();
    }
}
