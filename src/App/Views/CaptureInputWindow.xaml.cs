using DofusSwitcher.Models;

// [WARN] UseWPF + UseWindowsForms exposent des types homonymes. Alias explicites vers WPF + notre Binding.
using Window = System.Windows.Window;
using Binding = DofusSwitcher.Models.Binding;
using Key = System.Windows.Input.Key;
using KeyEventArgs = System.Windows.Input.KeyEventArgs;
using KeyInterop = System.Windows.Input.KeyInterop;
using Keyboard = System.Windows.Input.Keyboard;
using ModifierKeys = System.Windows.Input.ModifierKeys;
using MouseButton = System.Windows.Input.MouseButton;
using MouseButtonEventArgs = System.Windows.Input.MouseButtonEventArgs;
using RoutedEventArgs = System.Windows.RoutedEventArgs;
using DependencyObject = System.Windows.DependencyObject;
using VisualTreeHelper = System.Windows.Media.VisualTreeHelper;

namespace DofusSwitcher.Views;

/// <summary>
/// Modale de capture d'une entrée unique (US-S04). Écoute les événements d'entrée de CETTE fenêtre
/// focalisée (<c>OnPreviewKeyDown</c>/<c>OnPreviewMouseDown</c>) — [ARCH] jamais de hook bas niveau
/// global (réservé Axe 6, anti-bot). La classification (rejet combinaison/modificateur, encodage) est
/// déléguée au helper pur <see cref="InputCapture"/> ; ce code-behind ne fait que traduire les
/// événements WPF. Résultat exposé par <see cref="Result"/> ; <c>DialogResult</c> distingue capture/annulation.
/// </summary>
public partial class CaptureInputWindow : Window
{
    private const string CombinationError = "Une seule entrée : les combinaisons ne sont pas acceptées.";

    /// <summary>Crée la modale libellée par l'action ciblée (« Compte suivant », un nom de personnage…).</summary>
    public CaptureInputWindow(string actionLabel)
    {
        InitializeComponent();
        TitleText.Text = $"Assigner une entrée — « {actionLabel} »";
    }

    /// <summary>Entrée captée si <c>DialogResult == true</c>, sinon indéfinie (annulation).</summary>
    public Binding? Result { get; private set; }

    protected override void OnPreviewKeyDown(KeyEventArgs e)
    {
        base.OnPreviewKeyDown(e);

        // Échap → annuler sans rien assigner (acceptance) ; laisser le clavier natif ailleurs sinon.
        var key = e.Key == Key.System ? e.SystemKey : e.Key;
        if (key == Key.Escape)
        {
            DialogResult = false;
            return;
        }

        var virtualKey = KeyInterop.VirtualKeyFromKey(key);

        // Modificateur pressé seul : attendre une vraie touche (ne pas assigner, ne pas alerter).
        if (InputCapture.IsModifierVirtualKey(virtualKey))
        {
            e.Handled = true;
            return;
        }

        // Une touche + un modificateur maintenu = combinaison → rejetée (D-01).
        if (Keyboard.Modifiers != ModifierKeys.None)
        {
            ErrorText.Text = CombinationError;
            e.Handled = true;
            return;
        }

        Accept(InputCapture.FromKey(virtualKey));
        e.Handled = true;
    }

    protected override void OnPreviewMouseDown(MouseButtonEventArgs e)
    {
        base.OnPreviewMouseDown(e);

        // Ne pas capturer le clic sur « Annuler » (sinon on assignerait le clic gauche).
        if (IsWithinCancel(e.OriginalSource)) return;

        if (Keyboard.Modifiers != ModifierKeys.None)
        {
            ErrorText.Text = CombinationError;
            e.Handled = true;
            return;
        }

        // Toute la souris est acceptée, y compris les boutons auxiliaires X1/X2 (mémoire projet).
        CapturedMouseButton? button = e.ChangedButton switch
        {
            MouseButton.Left => CapturedMouseButton.Left,
            MouseButton.Right => CapturedMouseButton.Right,
            MouseButton.Middle => CapturedMouseButton.Middle,
            MouseButton.XButton1 => CapturedMouseButton.XButton1,
            MouseButton.XButton2 => CapturedMouseButton.XButton2,
            _ => null,
        };
        if (button is null) return;

        Accept(InputCapture.ToBinding(button.Value));
        e.Handled = true;
    }

    private void Accept(Binding binding)
    {
        Result = binding;
        DialogResult = true; // ferme la modale (ShowDialog → true).
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e) => DialogResult = false;

    private bool IsWithinCancel(object? source)
    {
        var current = source as DependencyObject;
        while (current is not null)
        {
            if (ReferenceEquals(current, CancelButton)) return true;
            current = VisualTreeHelper.GetParent(current);
        }
        return false;
    }
}
