using System.Collections.ObjectModel;
using DofusSwitcher.Models;
using DofusSwitcher.Services;

// [WARN] App référence WinForms : Binding est ambigu (System.Windows.Forms.Binding). Alias explicite.
using Binding = DofusSwitcher.Models.Binding;

namespace DofusSwitcher.ViewModels;

/// <summary>
/// ViewModel de l'onglet Comptes : liste temps réel des comptes avec leur état (US-D02), plus le
/// réordonnancement (US-D03) et l'exclusion/réintégration (US-D04, RG-D04).
/// S'abonne à <see cref="IWindowDetector"/> et, à chaque apparition/disparition, recalcule la vue via
/// <see cref="AccountMerge"/> puis réconcilie <see cref="Items"/> par clé <c>characterName</c> (les
/// instances d'items — donc la sélection — suivent le réordonnancement).
/// [ARCH] Aucun type WPF ni Dispatcher : le détecteur livre déjà sur le thread UI, ce VM reste testable
/// avec un faux détecteur. La persistance de l'ordre/exclusion sort par <see cref="AccountsChanged"/>,
/// que le <see cref="MainViewModel"/> relaie à l'autosave débouncé (ref [DT-006]).
/// [DECISION] Toute action utilisateur (réordonner/exclure) matérialise l'ordre visible complet dans
/// la config (ordre affiché = ordre persisté), y compris les comptes jusque-là seulement détectés
/// (ref [DT-009]). Avant toute action, un compte détecté non touché reste runtime only.
/// </summary>
public sealed class AccountsViewModel : ObservableObject
{
    private readonly Dictionary<string, DetectedWindow> _detected = new(StringComparer.Ordinal);
    private readonly ISessionProcessService? _session;
    private readonly Action? _requestShutdown;
    private readonly Action? _onSessionOpened;
    private readonly Action<string, Binding?>? _applyAccountDirectBinding;
    private List<AccountConfig> _persisted;
    private List<GameAccount> _gameAccounts;
    private AccountItemViewModel? _selectedItem;
    private bool _hasConnectedClients;
    private bool _showOpenSession;
    private string? _configuredLauncherPath;
    private string? _launcherPath;
    private bool _launcherResolved;
    private bool _reorderPending;

    /// <summary>
    /// Câble le VM sur les comptes persistés et le détecteur. L'abonnement précède <see cref="IWindowDetector.Start"/>
    /// (composition root) pour capter l'énumération initiale. Le service de session et le rappel de fermeture
    /// (Axe 9) sont optionnels : les tests VM qui n'exercent pas le cycle de session les omettent.
    /// </summary>
    public AccountsViewModel(
        IReadOnlyList<AccountConfig> persisted,
        IWindowDetector detector,
        IReadOnlyList<GameAccount>? gameAccounts = null,
        ISessionProcessService? session = null,
        Action? requestShutdown = null,
        Action<string, Binding?>? applyAccountDirectBinding = null,
        Action? onSessionOpened = null)
    {
        _persisted = [.. persisted];
        _gameAccounts = [.. gameAccounts ?? []];
        _session = session;
        _requestShutdown = requestShutdown;
        _onSessionOpened = onSessionOpened;
        _applyAccountDirectBinding = applyAccountDirectBinding;
        MoveUpCommand = new RelayCommand(() => MoveSelected(-1), () => CanMoveSelected(-1));
        MoveDownCommand = new RelayCommand(() => MoveSelected(+1), () => CanMoveSelected(+1));
        ToggleExcludeCommand = new RelayCommand(ToggleExcludeSelected, () => SelectedItem is not null);
        ToggleExcludeItemCommand = new RelayCommand<AccountItemViewModel>(ToggleExclude);
        OpenSessionCommand = new RelayCommand(OpenSession, () => _session is not null && LauncherPath() is not null);
        CloseClientCommand = new RelayCommand<AccountItemViewModel>(CloseClient);
        EndSessionCommand = new RelayCommand(EndSession, () => HasConnectedClients);

        detector.AccountAppeared += OnAccountAppeared;
        detector.AccountDisappeared += OnAccountDisappeared;
        Rebuild(); // état initial : comptes persistés, tous absents tant que rien n'est détecté.
    }

    /// <summary>
    /// Liste unifiée des personnages actuellement connectés (Axe 10, [DT-029]), liés ou non, dans l'ordre
    /// de rotation (ordre de <see cref="Items"/>). Fusionne les anciennes zones 1 (comptes connectés) et 2
    /// (personnages sans compte) : tout personnage connecté est un participant de plein droit (glisser,
    /// exclusion, activation directe) ; une ligne non liée porte en plus l'affordance de liaison.
    /// </summary>
    public ObservableCollection<ConnectedRowViewModel> ConnectedRows { get; } = [];

    /// <summary>Zone du bas — noms des comptes ayant des personnages liés mais aucun connecté (nom seul).</summary>
    public ObservableCollection<string> DisconnectedAccounts { get; } = [];

    /// <summary>Comptes disponibles pour la liaison (sans personnage lié connecté, RG-C03) — partagée par les lignes de zone 2.</summary>
    public ObservableCollection<string> AvailableAccounts { get; } = [];

    /// <summary>Émis après recomposition des zones (changement d'état runtime) — l'onglet Raccourcis rafraîchit ses libellés.</summary>
    public event Action? RuntimeChanged;

    /// <summary>
    /// Noms des personnages actuellement connectés et <b>non liés</b> à un compte (Axe 10), dans l'ordre de
    /// rotation. Pilotent les emplacements d'activation directe « sans compte » de l'onglet Raccourcis.
    /// </summary>
    public IReadOnlyList<string> ConnectedUnlinkedCharacters() =>
        ConnectedRows.Where(r => !r.IsLinked && !r.IsAnonymous).Select(r => r.Character.CharacterName).ToList();

    /// <summary>Comptes affichés, ordonnés (ordre persistant puis détectés non persistés en fin).</summary>
    public ObservableCollection<AccountItemViewModel> Items { get; } = [];

    /// <summary>Vrai quand aucun compte n'est connu (persisté ou détecté) : pilote l'état vide de la vue.</summary>
    public bool IsEmpty => Items.Count == 0;

    /// <summary>Inverse de <see cref="IsEmpty"/> : pilote la visibilité de la liste (converter in-box, sans inversion).</summary>
    public bool HasItems => Items.Count > 0;

    /// <summary>Liste des connectés non vide (au moins un personnage connecté) — pilote la visibilité de la section.</summary>
    public bool HasConnected => ConnectedRows.Count > 0;

    /// <summary>Zone du bas non vide (au moins un compte déconnecté à personnages liés).</summary>
    public bool HasDisconnected => DisconnectedAccounts.Count > 0;

    /// <summary>
    /// Vrai si au moins un client DOFUS est actuellement connecté (Axe 9) — pilote « Terminer session », les
    /// croix, et le masquage de « Ouvrir une session » (Axe 11). Inclut les clients <b>sans personnage</b> :
    /// une fenêtre DOFUS ouverte, même à l'écran de sélection, suffit à considérer une session en cours.
    /// </summary>
    public bool HasConnectedClients
    {
        get => _hasConnectedClients;
        private set { if (SetProperty(ref _hasConnectedClients, value)) EndSessionCommand.RaiseCanExecuteChanged(); }
    }

    /// <summary>
    /// Vrai quand aucun client n'est connecté ET aucun launcher n'est ouvert (Axe 9) : pilote la visibilité
    /// du gros bouton « Ouvrir une session ». Sans service de session (tests VM purs), toujours faux.
    /// </summary>
    public bool ShowOpenSession
    {
        get => _showOpenSession;
        private set => SetProperty(ref _showOpenSession, value);
    }

    /// <summary>Compte sélectionné dans la liste — cible des commandes ↑/↓/exclure (lié à la ListBox).</summary>
    public AccountItemViewModel? SelectedItem
    {
        get => _selectedItem;
        set { if (SetProperty(ref _selectedItem, value)) RaiseCommandStates(); }
    }

    /// <summary>Monte le compte sélectionné d'un rang (US-D03). Désactivée si déjà en tête.</summary>
    public RelayCommand MoveUpCommand { get; }

    /// <summary>Descend le compte sélectionné d'un rang (US-D03). Désactivée si déjà en fin.</summary>
    public RelayCommand MoveDownCommand { get; }

    /// <summary>Bascule l'exclusion du compte sélectionné (US-D04). Désactivée sans sélection.</summary>
    public RelayCommand ToggleExcludeCommand { get; }

    /// <summary>
    /// Bascule l'exclusion d'un compte donné (US-D04), porté par le bouton « œil » de sa ligne — cible
    /// l'item passé en paramètre, indépendamment de la sélection courante.
    /// </summary>
    public RelayCommand<AccountItemViewModel> ToggleExcludeItemCommand { get; }

    /// <summary>
    /// Lance l'Ankama Launcher (Axe 9, cycle de session). Désactivée sans service de session ou si le chemin
    /// du launcher est introuvable (bouton affiché mais grisé). Ref exception C-02 [DT-027].
    /// </summary>
    public RelayCommand OpenSessionCommand { get; }

    /// <summary>Force-kill le processus du client passé en paramètre (croix rouge par ligne connectée, Axe 9).</summary>
    public RelayCommand<AccountItemViewModel> CloseClientCommand { get; }

    /// <summary>Force-kill tous les clients connectés puis ferme réellement l'application (Axe 9).</summary>
    public RelayCommand EndSessionCommand { get; }

    /// <summary>
    /// Émis après une modification utilisateur (ordre/exclusion) avec la liste ordonnée à persister.
    /// La détection seule ne l'émet pas (l'état connecté/absent n'est jamais persisté).
    /// </summary>
    public event Action<IReadOnlyList<AccountConfig>>? AccountsChanged;

    /// <summary>
    /// Déplace un compte d'un index à un autre (glisser-déposer). Indices dans <see cref="Items"/>.
    /// [ARCH] Chemin unique de réordonnancement appelé par les handlers DnD du code-behind.
    /// </summary>
    public void MoveItem(int from, int to)
    {
        if (from == to || from < 0 || to < 0 || from >= Items.Count || to >= Items.Count) return;

        MaterializeFromItems(); // ordre visible → config (indices alignés sur Items)
        var moved = _persisted[from];
        _persisted.RemoveAt(from);
        _persisted.Insert(to, moved);
        RebuildAndPersist();
    }

    /// <summary>
    /// Prévisualise le réordonnancement pendant le glisser : réinsère l'item glissé juste avant ou après
    /// la cible (<paramref name="insertAfter"/> = moitié basse de la ligne survolée) et rafraîchit les zones,
    /// <b>sans</b> matérialiser ni persister. La ligne glissée occupe alors visuellement l'emplacement cible
    /// (rendue « fantôme » par <see cref="AccountItemViewModel.IsDragging"/>) et les autres se décalent.
    /// [DECISION] Règle d'insertion stable (destination = index d'insertion ajusté du retrait) pour éviter
    /// l'oscillation quand on survole une même ligne — le déplacement n'a lieu qu'au franchissement du milieu.
    /// [WARN] Réordonne <see cref="Items"/> en place ; l'ordre n'est figé qu'au lâcher via <see cref="CommitReorder"/>.
    /// Un glisser annulé est rétabli par <see cref="CancelReorder"/> (l'ordre persisté n'a pas bougé).
    /// </summary>
    public void PreviewReorder(AccountItemViewModel dragged, AccountItemViewModel target, bool insertAfter)
    {
        if (ReferenceEquals(dragged, target)) return;

        // [DECISION] Fluidité du glisser (Axe 10) : on déplace les DEUX collections EN PLACE
        // (ObservableCollection.Move → les conteneurs WPF sont conservés, pas de reconstruction ni de
        // clignotement), au lieu de recomposer toutes les zones à chaque franchissement. ConnectedRows porte
        // l'affichage ; Items porte l'ordre de rotation persisté (figé au lâcher par CommitReorder).
        var rowFrom = IndexOfRow(dragged);
        var rowTo = IndexOfRow(target);
        if (rowFrom < 0 || rowTo < 0) return;
        var rowInsertAt = insertAfter ? rowTo + 1 : rowTo;
        var rowDest = rowFrom < rowInsertAt ? rowInsertAt - 1 : rowInsertAt;
        if (rowDest == rowFrom || rowDest < 0 || rowDest >= ConnectedRows.Count) return;

        ConnectedRows.Move(rowFrom, rowDest);

        // Même déplacement dans Items (indices globaux — Items contient aussi les personnages absents).
        var itemFrom = Items.IndexOf(dragged);
        var itemTarget = Items.IndexOf(target);
        if (itemFrom >= 0 && itemTarget >= 0)
        {
            var itemInsertAt = insertAfter ? itemTarget + 1 : itemTarget;
            var itemDest = itemFrom < itemInsertAt ? itemInsertAt - 1 : itemInsertAt;
            if (itemDest != itemFrom && itemDest >= 0 && itemDest < Items.Count) Items.Move(itemFrom, itemDest);
        }

        _reorderPending = true;
    }

    /// <summary>Index d'une ligne connectée par identité de personnage (le DnD raisonne en <see cref="AccountItemViewModel"/>).</summary>
    private int IndexOfRow(AccountItemViewModel character)
    {
        for (var i = 0; i < ConnectedRows.Count; i++)
            if (ReferenceEquals(ConnectedRows[i].Character, character)) return i;
        return -1;
    }

    /// <summary>
    /// Fige l'ordre prévisualisé au lâcher (glisser-déposer) : matérialise l'ordre visible et persiste,
    /// via le même chemin que <see cref="MoveItem"/>. No-op si aucun déplacement n'a eu lieu (lâcher sur place).
    /// </summary>
    public void CommitReorder()
    {
        if (!_reorderPending) return;
        _reorderPending = false;
        MaterializeFromItems();
        RebuildAndPersist();
    }

    /// <summary>
    /// Rétablit l'ordre d'origine si le glisser est annulé (Échap, lâcher hors cible). L'ordre persisté
    /// n'ayant pas été touché, <see cref="Rebuild"/> restaure <see cref="Items"/> depuis <c>_persisted</c>.
    /// </summary>
    public void CancelReorder()
    {
        if (!_reorderPending) return;
        _reorderPending = false;
        Rebuild();
    }

    /// <summary>
    /// Remplace l'ensemble des comptes persistés (import de configuration, US-P04) et recalcule l'affichage.
    /// [DECISION] N'émet pas <see cref="AccountsChanged"/> : l'import est appliqué en bloc par le
    /// <see cref="MainViewModel"/>, qui persiste la config complète — éviter une double écriture. L'état
    /// runtime (comptes détectés/connectés) est préservé par la fusion sur les nouveaux comptes.
    /// </summary>
    public void LoadPersisted(IReadOnlyList<AccountConfig> persisted)
    {
        _persisted = [.. persisted];
        Rebuild();
    }

    /// <summary>
    /// Affecte (ou efface) l'activation directe d'un compte par son nom (Axe 5) — chemin unique de
    /// mutation du <c>DirectBinding</c>. Matérialise l'ordre visible d'abord (comme les autres actions,
    /// ref [DT-013]) pour que le compte, s'il n'était que détecté, soit persisté avec son binding, puis
    /// émet <see cref="AccountsChanged"/>. Sans compte correspondant : no-op.
    /// </summary>
    public void SetDirectBinding(string characterName, Binding? binding)
    {
        MaterializeFromItems();
        var index = _persisted.FindIndex(a => a.CharacterName == characterName);
        if (index < 0) return;
        _persisted[index] = _persisted[index] with { DirectBinding = binding };
        RebuildAndPersist();
    }

    private void OnAccountAppeared(DetectedWindow window)
    {
        _detected[window.CharacterName] = window;
        Rebuild();
    }

    private void OnAccountDisappeared(DetectedWindow window)
    {
        // Ne retirer que si le handle courant correspond : un renommage a déjà réémis l'apparition.
        if (_detected.TryGetValue(window.CharacterName, out var current) && current.Handle == window.Handle)
        {
            _detected.Remove(window.CharacterName);
            Rebuild();
        }
    }

    private bool CanMoveSelected(int direction)
    {
        if (SelectedItem is null) return false;
        var index = Items.IndexOf(SelectedItem);
        if (index < 0) return false;
        var target = index + direction;
        return target >= 0 && target < Items.Count;
    }

    private void MoveSelected(int direction)
    {
        if (SelectedItem is null) return;
        var index = Items.IndexOf(SelectedItem);
        MoveItem(index, index + direction);
    }

    private void ToggleExcludeSelected()
    {
        if (SelectedItem is not null) ToggleExclude(SelectedItem);
    }

    /// <summary>Bascule l'exclusion du compte donné (chemin partagé toolbar/sélection et bouton « œil » par ligne).</summary>
    private void ToggleExclude(AccountItemViewModel item)
    {
        MaterializeFromItems();
        var index = _persisted.FindIndex(a => a.CharacterName == item.CharacterName);
        if (index < 0) return;
        _persisted[index] = _persisted[index] with { Excluded = !_persisted[index].Excluded };
        RebuildAndPersist();
    }

    /// <summary>
    /// Fige l'ordre visible courant dans <see cref="_persisted"/> (ordre affiché = ordre persisté),
    /// matérialisant les comptes seulement détectés. Préserve <c>Excluded</c> et le <c>DirectBinding</c>
    /// existant par clé <c>characterName</c>.
    /// </summary>
    private void MaterializeFromItems()
    {
        var existingByName = _persisted.ToDictionary(a => a.CharacterName, StringComparer.Ordinal);
        var rebuilt = new List<AccountConfig>(Items.Count);
        foreach (var item in Items)
        {
            // Clients sans personnage (Axe 11) : purement runtime, jamais matérialisés en config ([DT-030]).
            if (!item.HasCharacter) continue;
            existingByName.TryGetValue(item.CharacterName, out var existing);
            rebuilt.Add(new AccountConfig(item.CharacterName, item.IsExcluded, existing?.DirectBinding, existing?.AccountName));
        }
        _persisted = rebuilt;
    }

    private void RebuildAndPersist()
    {
        Rebuild();
        AccountsChanged?.Invoke(_persisted);
    }

    /// <summary>Recalcule la fusion et réconcilie <see cref="Items"/> en place (clé = nom de personnage).</summary>
    private void Rebuild()
    {
        var merged = AccountMerge.Merge(_persisted, _detected.Values);

        // 1. Retirer les comptes qui ne sont plus dans la fusion.
        for (var i = Items.Count - 1; i >= 0; i--)
            if (!merged.Any(m => m.CharacterName == Items[i].CharacterName))
                Items.RemoveAt(i);

        // 2. Ajouter/mettre à jour et ordonner selon la fusion.
        for (var i = 0; i < merged.Count; i++)
        {
            var state = merged[i];
            var existing = Items.FirstOrDefault(x => x.CharacterName == state.CharacterName);
            if (existing is null)
            {
                Items.Insert(i, new AccountItemViewModel(state));
            }
            else
            {
                existing.Apply(state);
                var currentIndex = Items.IndexOf(existing);
                if (currentIndex != i) Items.Move(currentIndex, i);
            }
        }

        // Numéro de rotation (1-based) = position dans la liste ; la vue regroupe les connectés en tête
        // sans toucher à cet ordre (le tri d'affichage retombe sur Number, ref AccountsView.xaml).
        for (var i = 0; i < Items.Count; i++)
            Items[i].Number = i + 1;

        RebuildZones();
        OnPropertyChanged(nameof(IsEmpty));
        OnPropertyChanged(nameof(HasItems));
        RaiseCommandStates();
    }

    /// <summary>
    /// Recompose les 3 zones (comptes-v2 §2.6) et la liste des comptes disponibles à partir de l'état
    /// runtime courant (<see cref="Items"/>) et des comptes déclarés. Reconstruction complète : les
    /// collections sont petites (nombre de clients/comptes) et les changements peu fréquents.
    /// </summary>
    private void RebuildZones()
    {
        var states = Items
            .Select(i => new AccountRuntimeState(i.CharacterName, i.IsConnected, i.IsExcluded, i.Handle, i.AccountName, i.HasCharacter))
            .ToList();
        // Les clients sans personnage (Axe 11) ne participent pas à la logique de comptes/zones (ni lien, ni
        // disponibilité) : seuls les personnages réels sont partitionnés.
        var characterStates = states.Where(s => s.HasCharacter).ToList();
        var zones = AccountZones.Build(_gameAccounts, characterStates);

        // Comptes disponibles : mis à jour en place (référence partagée par les lignes de zone 2).
        AvailableAccounts.Clear();
        foreach (var name in zones.AvailableAccounts) AvailableAccounts.Add(name);

        // Liste unifiée « connectés » (Axe 10) ordonnée par l'ordre de rotation (ordre de Items) : lignes
        // liées (compte + perso), non liées (perso + liaison) et anonymes (client sans perso « Dofus N »,
        // Axe 11) mêlées, chacune de plein droit dans la rotation ([DT-029]/[DT-030]).
        var accountByChar = zones.Connected.ToDictionary(z => z.ConnectedCharacter, z => z.AccountName, StringComparer.Ordinal);
        var unlinked = new HashSet<string>(zones.UnlinkedConnected, StringComparer.Ordinal);
        ConnectedRows.Clear();
        var anonymousNumber = 0;
        foreach (var item in Items)
        {
            if (!item.HasCharacter)
                ConnectedRows.Add(new ConnectedRowViewModel(item, $"Dofus {++anonymousNumber}"));
            else if (accountByChar.TryGetValue(item.CharacterName, out var accountName))
                ConnectedRows.Add(new ConnectedRowViewModel(accountName, item));
            else if (unlinked.Contains(item.CharacterName))
                ConnectedRows.Add(new ConnectedRowViewModel(item, AvailableAccounts, LinkCharacter));
        }

        DisconnectedAccounts.Clear();
        foreach (var z in zones.Disconnected)
            DisconnectedAccounts.Add(z.AccountName);

        OnPropertyChanged(nameof(HasConnected));
        OnPropertyChanged(nameof(HasDisconnected));
        RefreshSessionState();
        RuntimeChanged?.Invoke();
    }

    /// <summary>
    /// Recalcule l'état du cycle de session (Axe 9) : présence de clients connectés et visibilité du bouton
    /// « Ouvrir une session ». [DECISION] La présence du launcher n'est sondée que lorsqu'aucun client n'est
    /// connecté (sinon le bouton est masqué de toute façon) et jamais en polling (anti-bot) : le
    /// rafraîchissement suit les apparitions/disparitions de fenêtres. Limitation : un launcher ouvert
    /// hors application, sans client, n'est détecté qu'au prochain événement fenêtre → recette.
    /// </summary>
    private void RefreshSessionState()
    {
        HasConnectedClients = Items.Any(i => i.IsConnected);
        // Axe 11 : « Ouvrir une session » proposé tant qu'AUCUNE fenêtre DOFUS n'est ouverte (client connecté
        // OU client sans personnage à l'écran de sélection) et qu'un chemin de launcher est utilisable — même si
        // le launcher tourne déjà sans client (dans ce cas l'action ramène sa fenêtre au premier plan).
        ShowOpenSession = _session is not null && !HasConnectedClients && LauncherPath() is not null;
    }

    /// <summary>
    /// Chemin du launcher résolu une fois puis mémoïsé : chemin configuré (Réglages) sinon auto-détection.
    /// Réévalué par <see cref="SetLauncherPath"/> quand l'utilisateur change le chemin.
    /// </summary>
    private string? LauncherPath()
    {
        if (!_launcherResolved)
        {
            _launcherPath = _session?.ResolveLauncherPath(_configuredLauncherPath);
            _launcherResolved = true;
        }
        return _launcherPath;
    }

    /// <summary>
    /// Applique le chemin de launcher configuré (Réglages, Axe 9) : réévalue la résolution et le gating du
    /// bouton. Appelé au démarrage (config initiale), sur modification dans les Réglages et à l'import.
    /// </summary>
    public void SetLauncherPath(string? configuredPath)
    {
        _configuredLauncherPath = configuredPath;
        _launcherResolved = false;
        RefreshSessionState();
        OpenSessionCommand.RaiseCanExecuteChanged();
    }

    private void OpenSession()
    {
        if (_session is null) return;

        if (_session.IsLauncherRunning())
        {
            // Launcher déjà ouvert (Axe 11) : ne pas en relancer un — ramener sa fenêtre au premier plan.
            _session.TryActivateLauncher();
        }
        else
        {
            var path = LauncherPath();
            if (path is null || !_session.LaunchLauncher(path)) return;
        }

        // Action « ouvrir une session » réussie : minimiser l'app si l'option est active (décidé côté MainViewModel).
        _onSessionOpened?.Invoke();
    }

    private void CloseClient(AccountItemViewModel? item)
    {
        if (item is not null) _session?.KillByHandle(item.Handle);
    }

    private void EndSession()
    {
        if (_session is null) return;
        // Snapshot : KillByHandle peut faire disparaître des fenêtres et muter Items pendant l'itération.
        foreach (var item in Items.Where(i => i.IsConnected).ToList())
            _session.KillByHandle(item.Handle);
        _requestShutdown?.Invoke();
    }

    /// <summary>
    /// Remplace la liste des comptes déclarés (après CRUD via le <see cref="MainViewModel"/>) et recompose
    /// les zones. Ne touche pas aux personnages : seul l'affichage des zones dépend des comptes.
    /// </summary>
    public void LoadGameAccounts(IReadOnlyList<GameAccount> gameAccounts)
    {
        _gameAccounts = [.. gameAccounts];
        RebuildZones();
    }

    /// <summary>
    /// Lie (ou délie si <paramref name="accountName"/> vaut <c>null</c>) un personnage à un compte — chemin
    /// unique de mutation du lien. Matérialise l'ordre visible (comme <see cref="SetDirectBinding"/>, ref
    /// [DT-013]) puis émet <see cref="AccountsChanged"/>. Sans personnage correspondant : no-op.
    /// </summary>
    public void LinkCharacter(string characterName, string? accountName)
    {
        MaterializeFromItems();
        var index = _persisted.FindIndex(a => a.CharacterName == characterName);
        if (index < 0) return;
        var current = _persisted[index];

        Binding? transferToAccount = null;
        if (accountName is not null && current.DirectBinding is not null)
        {
            // [DECISION] Au lien, l'activation directe passe de portée-personnage (Axe 10, non lié) à
            // portée-compte ([DT-025]/[DT-029]) : on transfère l'entrée au compte s'il n'en a pas encore,
            // sinon on l'efface simplement. L'unicité globale garantit l'absence de conflit dans les deux cas.
            var account = _gameAccounts.FirstOrDefault(a => NameEquals(a.Name, accountName));
            if (account is not null && account.DirectBinding is null) transferToAccount = current.DirectBinding;
            current = current with { DirectBinding = null };
        }

        _persisted[index] = current with { AccountName = accountName };
        RebuildAndPersist();

        // Écriture du DirectBinding côté compte via le seul writer des GameAccounts (MainViewModel).
        if (transferToAccount is not null) _applyAccountDirectBinding?.Invoke(accountName!, transferToAccount);
    }

    private static bool NameEquals(string a, string b) => string.Equals(a, b, StringComparison.OrdinalIgnoreCase);

    private void RaiseCommandStates()
    {
        MoveUpCommand.RaiseCanExecuteChanged();
        MoveDownCommand.RaiseCanExecuteChanged();
        ToggleExcludeCommand.RaiseCanExecuteChanged();
    }
}
