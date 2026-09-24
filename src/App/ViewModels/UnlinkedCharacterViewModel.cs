using System.Collections.ObjectModel;

namespace DofusSwitcher.ViewModels;

/// <summary>
/// Zone 2 de l'onglet Comptes : un personnage connecté non rattaché à un compte, avec l'action de
/// liaison. La liste des comptes proposés est partagée (référence <see cref="AvailableAccounts"/> tenue
/// à jour par l'<see cref="AccountsViewModel"/>) et ne contient que les comptes disponibles (sans
/// personnage lié connecté, RG-C03).
/// </summary>
public sealed class UnlinkedCharacterViewModel : ObservableObject
{
    private readonly Action<string, string> _link;
    private string? _selectedAccount;

    /// <summary>Câble la ligne sur le personnage, la liste partagée des comptes disponibles et le rappel de liaison.</summary>
    public UnlinkedCharacterViewModel(
        AccountItemViewModel character,
        ObservableCollection<string> availableAccounts,
        Action<string, string> link)
    {
        Character = character;
        AvailableAccounts = availableAccounts;
        _link = link;
        LinkCommand = new RelayCommand(DoLink, () => _selectedAccount is not null);
    }

    /// <summary>Personnage connecté à rattacher (porte son propre état/actions).</summary>
    public AccountItemViewModel Character { get; }

    /// <summary>Comptes proposés (disponibles) — référence partagée, mise à jour en place.</summary>
    public ObservableCollection<string> AvailableAccounts { get; }

    /// <summary>Compte choisi dans la liste (lié à la sélection du ComboBox).</summary>
    public string? SelectedAccount
    {
        get => _selectedAccount;
        set { if (SetProperty(ref _selectedAccount, value)) LinkCommand.RaiseCanExecuteChanged(); }
    }

    /// <summary>Rattache le personnage au compte choisi. Désactivée tant qu'aucun compte n'est sélectionné.</summary>
    public RelayCommand LinkCommand { get; }

    private void DoLink()
    {
        if (_selectedAccount is not null) _link(Character.CharacterName, _selectedAccount);
    }
}
