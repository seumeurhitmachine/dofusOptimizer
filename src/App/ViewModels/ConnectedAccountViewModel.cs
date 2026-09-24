namespace DofusSwitcher.ViewModels;

/// <summary>
/// Zone 1 de l'onglet Comptes : un compte connecté = le nom du compte + le personnage lié
/// actuellement connecté (affiché en sous-titre). Le personnage porte son propre état (exclusion,
/// bouton œil) via l'instance partagée <see cref="Character"/>.
/// </summary>
public sealed class ConnectedAccountViewModel(string accountName, AccountItemViewModel character)
{
    /// <summary>Nom du compte (titre de la ligne).</summary>
    public string AccountName { get; } = accountName;

    /// <summary>Personnage lié actuellement connecté (sous-titre + actions par personnage).</summary>
    public AccountItemViewModel Character { get; } = character;
}
