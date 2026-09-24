using System.Collections.ObjectModel;

namespace DofusSwitcher.ViewModels;

/// <summary>
/// Ligne de gestion d'un compte dans l'onglet Réglages (Axe 8) : nom (non modifiable) + suppression du
/// compte, et liste dépliable des personnages liés (chacun supprimable). Le déclic/flèche bascule
/// <see cref="IsExpanded"/>. Suppression compte/personnage via les rappels du <see cref="SettingsViewModel"/>
/// (→ MainViewModel, seul writer). Après mutation, la liste est reconstruite (cette instance remplacée).
/// </summary>
public sealed class GameAccountRowViewModel : ObservableObject
{
    private bool _isExpanded;

    /// <summary>Câble la ligne sur le nom, ses personnages liés et les rappels de suppression.</summary>
    public GameAccountRowViewModel(
        string name,
        IReadOnlyList<string> linkedCharacters,
        Action<string> deleteAccount,
        Action<string> deleteCharacter)
    {
        Name = name;
        LinkedCharacters = [.. linkedCharacters.Select(c => new LinkedCharacterViewModel(c, deleteCharacter))];
        DeleteCommand = new RelayCommand(() => deleteAccount(name));
        ToggleExpandCommand = new RelayCommand(() => IsExpanded = !IsExpanded);
    }

    /// <summary>Nom du compte (non modifiable).</summary>
    public string Name { get; }

    /// <summary>Personnages liés à ce compte (affichés une fois déplié).</summary>
    public ObservableCollection<LinkedCharacterViewModel> LinkedCharacters { get; }

    /// <summary>Vrai si le compte a des personnages liés (pilote l'affichage de la flèche/liste).</summary>
    public bool HasLinkedCharacters => LinkedCharacters.Count > 0;

    /// <summary>Vrai quand la liste des personnages liés est dépliée.</summary>
    public bool IsExpanded
    {
        get => _isExpanded;
        set => SetProperty(ref _isExpanded, value);
    }

    /// <summary>Déplie/replie la liste des personnages liés (clic sur le nom ou la flèche).</summary>
    public RelayCommand ToggleExpandCommand { get; }

    /// <summary>Supprime le compte et ses personnages liés, sans confirmation (US-C03, RG-C04).</summary>
    public RelayCommand DeleteCommand { get; }
}
