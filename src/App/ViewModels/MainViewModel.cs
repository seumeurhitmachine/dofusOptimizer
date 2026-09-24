namespace DofusSwitcher.ViewModels;

/// <summary>
/// ViewModel racine de la fenêtre principale.
/// Axe 1 : shell vide (titre de fenêtre). Les onglets Comptes / Raccourcis / Réglages
/// recevront leurs propres ViewModels aux Axes suivants (3/5/7).
/// </summary>
public sealed class MainViewModel : ObservableObject
{
    /// <summary>Titre affiché dans la barre de la fenêtre.</summary>
    public string Title => "Dofus Window Switcher";
}
