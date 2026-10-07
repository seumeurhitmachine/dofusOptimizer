using System.Collections.ObjectModel;

namespace DofusSwitcher.ViewModels;

/// <summary>
/// Ligne unifiée de la liste « connectés » (Axe 10) : un personnage DOFUS actuellement connecté, lié à un
/// compte <b>ou non</b>. Toutes les lignes portent les actions par personnage (exclusion, fermeture,
/// glisser-déposer) via l'instance partagée <see cref="Character"/> ; une ligne <b>non liée</b> expose en
/// plus l'affordance de liaison (liste des comptes disponibles + « Lier »). Fusionne les anciens
/// <c>ConnectedAccountViewModel</c> (zone 1) et <c>UnlinkedCharacterViewModel</c> (zone 2) : les
/// personnages sans compte sont désormais des participants de plein droit à la rotation ([DT-029]).
/// </summary>
public sealed class ConnectedRowViewModel : ObservableObject
{
    private readonly Action<string, string>? _link;
    private string? _selectedAccount;

    /// <summary>
    /// Ligne <b>liée</b> : nom du compte + personnage connecté (pas d'affordance de liaison). Le drapeau
    /// <paramref name="isChef"/> marque le personnage du compte chef (Axe 13) ; <paramref name="copyInvite"/>
    /// alimente la commande de copie des <c>/invite</c> (affichée seulement si au moins un autre perso est connecté).
    /// </summary>
    public ConnectedRowViewModel(
        string accountName,
        AccountItemViewModel character,
        bool isChef = false,
        bool canInvite = false,
        Action? copyInvite = null)
    {
        AccountName = accountName;
        Character = character;
        IsChef = isChef;
        CanInvite = canInvite;
        AvailableAccounts = [];
        LinkCommand = new RelayCommand(DoLink, () => CanLink);
        CopyInviteCommand = new RelayCommand(() => copyInvite?.Invoke());
    }

    /// <summary>Ligne <b>non liée</b> : personnage connecté sans compte + liaison à un compte disponible.</summary>
    public ConnectedRowViewModel(
        AccountItemViewModel character,
        ObservableCollection<string> availableAccounts,
        Action<string, string> link)
    {
        Character = character;
        AvailableAccounts = availableAccounts;
        _link = link;
        LinkCommand = new RelayCommand(DoLink, () => CanLink);
        CopyInviteCommand = new RelayCommand(() => { });
    }

    /// <summary>
    /// Ligne <b>anonyme</b> (Axe 11) : client DOFUS connecté <b>sans personnage</b> (écran de sélection). Aucune
    /// affordance de personnage — ni glisser, ni exclusion, ni compte, ni raccourci : uniquement un libellé
    /// « Dofus N » (<paramref name="displayName"/>) et la fermeture (croix). Toujours dans la rotation ([DT-030]).
    /// </summary>
    public ConnectedRowViewModel(AccountItemViewModel character, string displayName)
    {
        Character = character;
        IsAnonymous = true;
        DisplayName = displayName;
        AvailableAccounts = [];
        LinkCommand = new RelayCommand(DoLink, () => CanLink);
        CopyInviteCommand = new RelayCommand(() => { });
    }

    /// <summary>Personnage connecté (nom, exclusion, glisser, fermeture) — instance partagée avec <c>Items</c>.</summary>
    public AccountItemViewModel Character { get; }

    /// <summary>Nom du compte lié (sous-titre), ou <c>null</c> si la ligne n'est pas rattachée à un compte.</summary>
    public string? AccountName { get; }

    /// <summary>Vrai pour un client connecté <b>sans personnage</b> (Axe 11) : rendu réduit (libellé + croix).</summary>
    public bool IsAnonymous { get; }

    /// <summary>Libellé affiché : « Dofus N » pour une ligne anonyme, sinon le nom du personnage.</summary>
    public string Title => IsAnonymous ? DisplayName! : Character.CharacterName;

    /// <summary>Libellé « Dofus N » d'une ligne anonyme (positionnel), sinon <c>null</c>.</summary>
    public string? DisplayName { get; }

    /// <summary>Vrai si la ligne est rattachée à un compte : pilote le rendu (sous-titre compte vs liaison).</summary>
    public bool IsLinked => AccountName is not null;

    /// <summary>Comptes disponibles pour la liaison (référence partagée, mise à jour en place) — vide si liée.</summary>
    public ObservableCollection<string> AvailableAccounts { get; }

    /// <summary>Compte choisi dans la liste déroulante (lignes non liées uniquement).</summary>
    public string? SelectedAccount
    {
        get => _selectedAccount;
        set
        {
            if (!SetProperty(ref _selectedAccount, value)) return;
            OnPropertyChanged(nameof(CanLink));
            LinkCommand.RaiseCanExecuteChanged();
        }
    }

    /// <summary>
    /// Vrai quand un compte est sélectionné sur une ligne non liée : pilote l'activation <b>et</b> la
    /// visibilité du bouton « Lier » (Axe 10 — masqué tant qu'aucun compte n'est choisi, plus seulement grisé).
    /// </summary>
    public bool CanLink => !IsLinked && _selectedAccount is not null;

    /// <summary>Rattache le personnage au compte choisi. Désactivée/masquée tant qu'aucun compte n'est sélectionné.</summary>
    public RelayCommand LinkCommand { get; }

    /// <summary>
    /// Vrai si ce personnage est celui du compte <b>chef</b> actuellement connecté (Axe 13). Pilote l'affichage
    /// de la couronne à droite du nom. Faux pour une ligne non liée ou anonyme.
    /// </summary>
    public bool IsChef { get; }

    /// <summary>Vrai s'il existe au moins un autre personnage connecté (non anonyme) à inviter depuis cette ligne chef.</summary>
    public bool CanInvite { get; }

    /// <summary>
    /// Affiche le bouton de copie des <c>/invite</c> : uniquement sur la ligne du chef (Axe 13) <b>et</b> s'il y a
    /// au moins un autre personnage connecté à inviter (sinon la formule serait vide → bouton masqué).
    /// </summary>
    public bool ShowInvite => IsChef && CanInvite;

    /// <summary>Copie dans le presse-papier la formule <c>/invite …</c> des autres personnages connectés (ligne chef, Axe 13).</summary>
    public RelayCommand CopyInviteCommand { get; }

    private void DoLink()
    {
        if (_selectedAccount is not null) _link?.Invoke(Character.CharacterName, _selectedAccount);
    }
}
