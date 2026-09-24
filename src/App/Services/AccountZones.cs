using DofusSwitcher.Models;

namespace DofusSwitcher.Services;

/// <summary>
/// Partition **pure** de l'onglet Comptes en 3 zones (spec comptes-v2 §2.6), plus la liste des comptes
/// **disponibles** pour la liaison. Déterministe, sans type WPF ni effet de bord — foyer testable, sur le
/// modèle de <see cref="AccountMerge"/>. Le ViewModel mappe ensuite ces noms vers ses instances observables.
/// </summary>
public static class AccountZones
{
    private static readonly StringComparer NameCmp = StringComparer.OrdinalIgnoreCase;

    /// <summary>
    /// Répartit les personnages (états runtime, incl. lien compte) selon les comptes déclarés.
    /// <list type="bullet">
    /// <item>Zone 1 — comptes ayant un personnage lié connecté (compte + ce personnage).</item>
    /// <item>Zone 2 — personnages connectés non rattachés à un compte existant.</item>
    /// <item>Zone 3 — comptes ayant des personnages liés mais aucun connecté.</item>
    /// </list>
    /// Un compte sans aucun personnage lié n'apparaît nulle part (géré via Réglages). Un compte est
    /// <b>disponible</b> pour la liaison s'il n'a pas de personnage lié connecté (RG-C03).
    /// </summary>
    public static AccountZoneResult Build(
        IReadOnlyList<GameAccount> accounts,
        IReadOnlyList<AccountRuntimeState> characters)
    {
        var accountNames = new HashSet<string>(accounts.Select(a => a.Name), NameCmp);

        var connected = new List<ConnectedAccountZone>();
        var disconnected = new List<DisconnectedAccountZone>();
        var available = new List<string>();

        foreach (var account in accounts)
        {
            var linked = characters
                .Where(c => c.AccountName is not null && NameCmp.Equals(c.AccountName, account.Name))
                .ToList();
            var connectedLinked = linked.Where(c => c.IsConnected).ToList();

            // Disponible pour lier un personnage : aucun personnage lié n'est déjà connecté (RG-C03).
            if (connectedLinked.Count == 0) available.Add(account.Name);

            if (connectedLinked.Count > 0)
                connected.Add(new ConnectedAccountZone(account.Name, connectedLinked[0].CharacterName));
            else if (linked.Count > 0)
                disconnected.Add(new DisconnectedAccountZone(account.Name, linked.Select(c => c.CharacterName).ToList()));
        }

        // Zone 2 : personnages connectés sans compte existant (non lié ou lien orphelin après suppression).
        var unlinkedConnected = characters
            .Where(c => c.IsConnected && (c.AccountName is null || !accountNames.Contains(c.AccountName)))
            .Select(c => c.CharacterName)
            .ToList();

        return new AccountZoneResult(connected, unlinkedConnected, disconnected, available);
    }
}

/// <summary>Résultat de <see cref="AccountZones.Build"/> — noms uniquement (le VM relie aux instances).</summary>
public sealed record AccountZoneResult(
    IReadOnlyList<ConnectedAccountZone> Connected,
    IReadOnlyList<string> UnlinkedConnected,
    IReadOnlyList<DisconnectedAccountZone> Disconnected,
    IReadOnlyList<string> AvailableAccounts);

/// <summary>Zone 1 : un compte et le nom de son personnage lié actuellement connecté.</summary>
public sealed record ConnectedAccountZone(string AccountName, string ConnectedCharacter);

/// <summary>Zone 3 : un compte déconnecté et les noms de ses personnages liés (tous absents).</summary>
public sealed record DisconnectedAccountZone(string AccountName, IReadOnlyList<string> LinkedCharacters);
