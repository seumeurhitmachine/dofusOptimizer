using DofusSwitcher.Models;

// [WARN] App référence WinForms : Binding est ambigu (System.Windows.Forms.Binding). Alias explicite.
using Binding = DofusSwitcher.Models.Binding;

namespace DofusSwitcher.ViewModels;

/// <summary>
/// Un emplacement assignable de l'onglet Raccourcis (suivant, précédent, ou activation directe d'un
/// compte) : libellé, entrée courante affichée, message de conflit inline et commandes Capturer/Effacer.
/// [ARCH] Aucune logique de conflit/persistance ici : le slot est piloté par <see cref="ShortcutsViewModel"/>
/// via les délégués <c>capture</c>/<c>clear</c> (qui portent l'identité de l'emplacement).
/// </summary>
public sealed class ShortcutSlotViewModel : ObservableObject
{
    private Binding? _binding;
    private string? _conflictMessage;
    private string _label;

    /// <summary>Crée un slot. <paramref name="capture"/>/<paramref name="clear"/> sont fournis par le VM parent.</summary>
    /// <param name="id">Identité stable pour la réconciliation (nom de personnage pour un slot direct), ou <c>null</c>.</param>
    public ShortcutSlotViewModel(string label, Action capture, Action clear, string? id = null)
    {
        _label = label;
        Id = id;
        CaptureCommand = new RelayCommand(capture);
        ClearCommand = new RelayCommand(clear, () => HasBinding);
    }

    /// <summary>Identité stable du slot (nom de personnage pour l'activation directe) ; <c>null</c> pour suivant/précédent.</summary>
    public string? Id { get; }

    /// <summary>Libellé affiché (« Compte suivant » ou nom du compte pour l'activation directe). Mutable : suit un re-rattachement.</summary>
    public string Label
    {
        get => _label;
        set => SetProperty(ref _label, value);
    }

    /// <summary>Entrée courante affichée (« XButton2 », « F1 »…) ou « non assignée ».</summary>
    public string Display => _binding is null ? "non assignée" : InputCapture.Format(_binding);

    /// <summary>Vrai si une entrée est assignée (pilote l'activation de « Effacer »).</summary>
    public bool HasBinding => _binding is not null;

    /// <summary>Message de conflit inline (error) sous l'association, ou <c>null</c> si aucun (RG-S05).</summary>
    public string? ConflictMessage
    {
        get => _conflictMessage;
        set { if (SetProperty(ref _conflictMessage, value)) OnPropertyChanged(nameof(HasConflict)); }
    }

    /// <summary>Vrai quand un conflit est signalé (pilote la visibilité de l'indicateur).</summary>
    public bool HasConflict => _conflictMessage is not null;

    /// <summary>Ouvre la modale de capture pour cet emplacement.</summary>
    public RelayCommand CaptureCommand { get; }

    /// <summary>Efface l'entrée assignée à cet emplacement.</summary>
    public RelayCommand ClearCommand { get; }

    /// <summary>Met à jour l'entrée courante affichée (appelé par le VM après persistance).</summary>
    public void SetBinding(Binding? binding)
    {
        _binding = binding;
        OnPropertyChanged(nameof(Display));
        OnPropertyChanged(nameof(HasBinding));
        ClearCommand.RaiseCanExecuteChanged();
    }
}
