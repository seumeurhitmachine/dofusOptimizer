using DofusSwitcher.Models;

namespace DofusSwitcher.ViewModels;

/// <summary>
/// Item de la liste des comptes : nom de personnage + état runtime (connecté/absent, exclu).
/// [ARCH] Zéro type WPF (pas de Brush/Visibility) : la conversion vers l'UI se fait par binding/
/// triggers dans <c>AccountsView.xaml</c> (archi §MVVM). Alimenté par <see cref="AccountRuntimeState"/>.
/// </summary>
public sealed class AccountItemViewModel : ObservableObject
{
    private bool _isConnected;
    private bool _isExcluded;
    private nint _handle;
    private int _number;
    private string? _accountName;

    /// <summary>Crée l'item à partir d'un état de fusion.</summary>
    public AccountItemViewModel(AccountRuntimeState state)
    {
        CharacterName = state.CharacterName;
        Apply(state);
    }

    /// <summary>Nom de personnage — clé naturelle, immuable pour la durée de vie de l'item (RG-D01).</summary>
    public string CharacterName { get; }

    /// <summary>Vrai si une fenêtre DOFUS correspondante existe actuellement (RG-D02).</summary>
    public bool IsConnected
    {
        get => _isConnected;
        private set => SetProperty(ref _isConnected, value);
    }

    /// <summary>Vrai si le compte est exclu de la rotation (conservé, EF-09).</summary>
    public bool IsExcluded
    {
        get => _isExcluded;
        private set => SetProperty(ref _isExcluded, value);
    }

    /// <summary>HWND courant si connecté, sinon <c>0</c>. Consommé à l'Axe 6 (bascule de focus).</summary>
    public nint Handle
    {
        get => _handle;
        private set => SetProperty(ref _handle, value);
    }

    /// <summary>
    /// Numéro d'ordre de rotation (1-based) = position dans la liste persistée. Affiché à gauche du nom.
    /// Sert aussi de clé de tri secondaire à la vue (comptes connectés en tête, puis par numéro croissant).
    /// Piloté par <see cref="AccountsViewModel.Rebuild"/> ; indépendant de l'état connecté.
    /// </summary>
    public int Number
    {
        get => _number;
        set => SetProperty(ref _number, value);
    }

    /// <summary>Nom du compte lié (v2), ou <c>null</c> si non lié. Pilote la répartition en 3 zones.</summary>
    public string? AccountName
    {
        get => _accountName;
        private set => SetProperty(ref _accountName, value);
    }

    /// <summary>
    /// Met à jour l'état runtime depuis une nouvelle fusion. Le nom (clé) doit correspondre :
    /// l'appelant réconcilie par <see cref="CharacterName"/>.
    /// </summary>
    public void Apply(AccountRuntimeState state)
    {
        IsConnected = state.IsConnected;
        IsExcluded = state.IsExcluded;
        Handle = state.Handle;
        AccountName = state.AccountName;
    }
}
