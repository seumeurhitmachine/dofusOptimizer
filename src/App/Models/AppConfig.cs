using DofusSwitcher.Constants;

namespace DofusSwitcher.Models;

/// <summary>
/// Racine de la configuration persistée en JSON (<c>%APPDATA%\DofusSwitcher\config.json</c>).
/// [ARCH] Toute évolution de schéma incrémente <see cref="SchemaVersion"/> et fournit une
/// migration montante idempotente — ne jamais casser une config existante (data-model §Conventions).
/// </summary>
/// <param name="SchemaVersion">Version du schéma (≥ 1). Voir <see cref="CurrentSchemaVersion"/>.</param>
/// <param name="InterceptionSuspended">
/// État persistant : quand vrai, le hook transmet toujours nativement l'entrée (RG-T03).
/// </param>
/// <param name="StartWithWindows">Option de démarrage automatique avec Windows (off par défaut).</param>
/// <param name="NextBinding">Entrée de bascule <c>suivant</c>, optionnelle et indépendante.</param>
/// <param name="PrevBinding">Entrée de bascule <c>précédent</c>, optionnelle et indépendante.</param>
/// <param name="Accounts">
/// Comptes ordonnés : l'ordre de la liste <b>est</b> l'ordre de rotation (source unique de
/// vérité — pas de champ <c>order</c> séparé, data-model §AppConfig).
/// </param>
public sealed record AppConfig(
    int SchemaVersion,
    bool InterceptionSuspended,
    bool StartWithWindows,
    Binding? NextBinding,
    Binding? PrevBinding,
    List<AccountConfig> Accounts)
{
    /// <summary>Version de schéma produite par cette version de l'app.</summary>
    public const int CurrentSchemaVersion = 1;

    /// <summary>
    /// Configuration vide par défaut : aucun compte, associations par défaut
    /// <c>suivant = XButton2</c> / <c>précédent = XButton1</c> (data-model §AppConfig).
    /// </summary>
    public static AppConfig Default => new(
        SchemaVersion: CurrentSchemaVersion,
        InterceptionSuspended: false,
        StartWithWindows: false,
        NextBinding: new Binding(BindingKind.MouseButton, AppConstants.DefaultNextButtonCode),
        PrevBinding: new Binding(BindingKind.MouseButton, AppConstants.DefaultPrevButtonCode),
        Accounts: []);

    /// <summary>
    /// Énumère toutes les entrées assignées (suivant, précédent, et les directes des comptes),
    /// en ignorant les emplacements non assignés. Ordre stable : globales puis comptes.
    /// </summary>
    public IEnumerable<Binding> AllBindings()
    {
        if (NextBinding is not null) yield return NextBinding;
        if (PrevBinding is not null) yield return PrevBinding;
        foreach (var account in Accounts)
            if (account.DirectBinding is not null) yield return account.DirectBinding;
    }

    /// <summary>
    /// Indique si deux actions partagent une même entrée physique (violation de l'unicité
    /// globale, EF-08). Le fichier persisté doit toujours être sans conflit ; le refus à la
    /// saisie est géré par l'UI (Axe 5) — cette méthode en est le foyer réutilisable.
    /// </summary>
    public bool HasBindingConflicts()
    {
        var seen = new HashSet<Binding>();
        foreach (var binding in AllBindings())
            if (!seen.Add(binding)) return true;
        return false;
    }
}
