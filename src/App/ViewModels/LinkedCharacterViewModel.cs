namespace DofusSwitcher.ViewModels;

/// <summary>
/// Un personnage lié affiché sous un compte déplié (Réglages, Axe 8) : nom + suppression du personnage.
/// La suppression retire la config persistée du personnage (s'il est connecté, il réapparaît non lié).
/// </summary>
public sealed class LinkedCharacterViewModel(string name, Action<string> delete)
{
    /// <summary>Nom du personnage lié.</summary>
    public string Name { get; } = name;

    /// <summary>Supprime ce personnage.</summary>
    public RelayCommand DeleteCommand { get; } = new(() => delete(name));
}
