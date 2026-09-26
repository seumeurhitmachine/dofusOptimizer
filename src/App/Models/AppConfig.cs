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
/// <b>Personnages</b> ordonnés (type <see cref="AccountConfig"/>, nom historique) : l'ordre de la liste
/// <b>est</b> l'ordre de rotation (source unique de vérité — pas de champ <c>order</c> séparé, data-model §AppConfig).
/// </param>
/// <param name="GameAccounts">
/// <b>Comptes</b> (v2) déclarés par l'utilisateur. Absent des configs v1 → migré en liste vide.
/// Le lien personnage→compte est porté par <see cref="AccountConfig.AccountName"/>.
/// </param>
public sealed record AppConfig(
    int SchemaVersion,
    bool InterceptionSuspended,
    bool StartWithWindows,
    Binding? NextBinding,
    Binding? PrevBinding,
    List<AccountConfig> Accounts,
    List<GameAccount> GameAccounts)
{
    /// <summary>
    /// Chemin de l'exécutable de l'Ankama Launcher (Axe 9, « Ouvrir une session »). Propriété additive hors
    /// constructeur positionnel : absente d'une config → <c>null</c> (schéma inchangé, pas de migration). Quand
    /// renseignée, prime sur l'auto-détection ; si aucun chemin utilisable, le bouton d'ouverture est masqué.
    /// </summary>
    public string? LauncherPath { get; init; }

    /// <summary>
    /// Cycle de vie fenêtre (Axe 9) : quand vrai (défaut), fermer la fenêtre [X] <b>minimise</b> l'application
    /// au lieu de terminer le processus ; quand faux, [X] quitte réellement. Une config antérieure à v3 n'a pas
    /// le champ (source-gen → <c>false</c>) : la migration v→3 le force à <c>true</c> (comportement tray préservé).
    /// </summary>
    public bool CloseMinimizes { get; init; } = true;

    /// <summary>
    /// Cycle de vie fenêtre (Axe 9) : quand vrai, minimiser (ou fermer si <see cref="CloseMinimizes"/>) masque
    /// la fenêtre dans la <b>barre d'état</b> (zone de notification) plutôt que dans la barre des tâches.
    /// Défaut faux (minimisation classique en barre des tâches).
    /// </summary>
    public bool MinimizeToTray { get; init; }

    /// <summary>
    /// Cycle de session (Axe 11) : quand vrai, cliquer « Ouvrir une session » <b>minimise</b> ensuite Dofus
    /// Optimizer (pour laisser la place au launcher). Propriété additive hors constructeur positionnel : absente
    /// d'une config → défaut naturel <c>false</c> (schéma inchangé, pas de migration — comme <see cref="LauncherPath"/>).
    /// </summary>
    public bool MinimizeOnOpenSession { get; init; }

    /// <summary>
    /// Version de schéma produite par cette version de l'app. v2 : comptes (v1 = personnages seuls).
    /// v3 : cycle de vie fenêtre (<see cref="CloseMinimizes"/>/<see cref="MinimizeToTray"/>).
    /// </summary>
    public const int CurrentSchemaVersion = 3;

    /// <summary>
    /// Configuration vide par défaut : aucun personnage, aucun compte, associations par défaut
    /// <c>suivant = XButton2</c> / <c>précédent = XButton1</c> (data-model §AppConfig).
    /// </summary>
    public static AppConfig Default => new(
        SchemaVersion: CurrentSchemaVersion,
        InterceptionSuspended: false,
        StartWithWindows: false,
        NextBinding: new Binding(BindingKind.MouseButton, AppConstants.DefaultNextButtonCode),
        PrevBinding: new Binding(BindingKind.MouseButton, AppConstants.DefaultPrevButtonCode),
        Accounts: [],
        GameAccounts: []);

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
        foreach (var game in GameAccounts)
            if (game.DirectBinding is not null) yield return game.DirectBinding;
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
