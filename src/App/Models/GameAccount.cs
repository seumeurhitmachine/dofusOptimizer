using System.Text.RegularExpressions;

namespace DofusSwitcher.Models;

/// <summary>
/// Compte DOFUS (v2) : unité d'organisation regroupant 0..N personnages (<see cref="AccountConfig"/>),
/// dont un seul peut être connecté à la fois (RG-C02/C03). Identité = <see cref="Name"/> (clé naturelle,
/// pas d'ID de substitution — cohérent avec le reste du modèle local).
/// [DECISION] Nom de type <c>GameAccount</c> pour éviter la collision avec <see cref="AccountConfig"/>
/// (personnage, nom historique). Aucun état runtime ici : « compte connecté » est calculé à partir des
/// personnages liés détectés (data-model §Runtime vs persisté).
/// </summary>
/// <param name="Name">Nom saisi par l'utilisateur : lettres/chiffres/espaces/tirets, 1..40, unique (RG-C01).</param>
/// <param name="DirectBinding">
/// Entrée d'activation directe du compte (Axe 8) : active le personnage lié actuellement connecté,
/// indépendamment duquel. Optionnelle ; soumise à l'unicité globale.
/// </param>
public sealed partial record GameAccount(string Name, Binding? DirectBinding = null)
{
    /// <summary>Longueur maximale d'un nom de compte (RG-C01).</summary>
    public const int MaxNameLength = 40;

    [GeneratedRegex(@"^[A-Za-z0-9 -]{1,40}$")]
    private static partial Regex NamePattern();

    /// <summary>
    /// Valide un nom de compte (RG-C01) : après <c>Trim</c>, non vide, ≤ 40, lettres/chiffres/espaces/tirets.
    /// L'unicité (insensible à la casse) est vérifiée au niveau de la collection, pas ici.
    /// </summary>
    public static bool IsValidName(string? name) =>
        name is not null && NamePattern().IsMatch(name.Trim());
}
