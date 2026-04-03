using RayCarrot.RCP.Metro.Games.Clients.BigPEmu;
using RayCarrot.RCP.Metro.Games.Clients.Custom;
using RayCarrot.RCP.Metro.Games.Clients.Dolphin;
using RayCarrot.RCP.Metro.Games.Clients.DosBox;
using RayCarrot.RCP.Metro.Games.Clients.MGba;
using RayCarrot.RCP.Metro.Games.Clients.Steam;
using RayCarrot.RCP.Metro.Games.Clients.UbisoftConnect;
using RayCarrot.RCP.Metro.Games.Components;
using RayCarrot.RCP.Metro.Games.Finder;

namespace RayCarrot.RCP.Metro.Games.Clients;

public class GameClientsManager
{
    #region Constructor

    public GameClientsManager(AppUserData data, IMessenger messenger, GamesManager gamesManager)
    {
        Data = data ?? throw new ArgumentNullException(nameof(data));
        Messenger = messenger ?? throw new ArgumentNullException(nameof(messenger));
        GamesManager = gamesManager ?? throw new ArgumentNullException(nameof(gamesManager));

        GameClientDescriptors = new GameClientDescriptor[]
        {
            // Emulators
            new CustomGameClientDescriptor(),
            new DosBoxGameClientDescriptor(),
            new BigPEmuGameClientDescriptor(),
            new DuckStationGameClientDescriptor(),
            new BizHawkGameClientDescriptor(),
            new PCSX2GameClientDescriptor(),
            new SameBoyGameClientDescriptor(),
            new MGbaGameClientDescriptor(),
            new VisualBoyAdvanceMGameClientDescriptor(),
            new DolphinGameClientDescriptor(),
            
            // Clients
            new SteamGameClientDescriptor(),
            new UbisoftConnectGameClientDescriptor(),
        }.ToDictionary(x => x.GameClientId);
        SortedGameClientDescriptors = GameClientDescriptors.Values.OrderBy(x => x).ToArray();
    }

    #endregion

    #region Logger

    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    #endregion

    #region Services

    private AppUserData Data { get; }
    private IMessenger Messenger { get; }
    private GamesManager GamesManager { get; }

    #endregion

    #region Private Properties

    private Dictionary<string, GameClientDescriptor> GameClientDescriptors { get; }
    private GameClientDescriptor[] SortedGameClientDescriptors { get; }

    #endregion

    #region Private Methods

    private async Task<GameClientInstallation> AddGameClientImplAsync(
        GameClientDescriptor descriptor,
        InstallLocation installLocation,
        ConfigureGameClientInstallation? configureInstallation = null)
    {
        GameClientInstallation installation = new(descriptor, installLocation);

        Data.Game_GameClientInstallations.AddSorted(installation);

        // Configure
        if (configureInstallation != null) 
            await configureInstallation.Invoke(installation);

        Logger.Info("The game client {0} has been added", installation.FullId);

        await descriptor.OnGameClientAddedAsync(installation);

        // Attempt to use this game client on games without one and which default to use one
        List<GameInstallation> modifiedGames = new();
        foreach (GameInstallation gameInstallation in GamesManager.GetInstalledGames())
        {
            if (gameInstallation.GameDescriptor.DefaultToUseGameClient &&
                descriptor.SupportsGame(gameInstallation, installation) &&
                GetAttachedGameClient(gameInstallation) == null)
            {
                await AttachGameClientImplAsync(gameInstallation, installation);
                modifiedGames.Add(gameInstallation);
            }
        }

        // Refresh all modified games in a single message
        if (modifiedGames.Any())
            Messenger.Send(new ModifiedGamesMessage(modifiedGames, rebuiltComponents: true));

        return installation;
    }

    private async Task RemoveGameClientImplAsync(GameClientInstallation gameClientInstallation)
    {
        Data.Game_GameClientInstallations.Remove(gameClientInstallation);

        // Deselect this game client from any games which use it
        List<GameInstallation> modifiedGames = new();
        foreach (GameInstallation gameInstallation in GamesManager.GetInstalledGames())
        {
            if (gameInstallation.GetValue<string>(GameDataKey.Client_AttachedClient) == gameClientInstallation.InstallationId)
            {
                if (gameInstallation.GameDescriptor.DefaultToUseGameClient)
                {
                    // Get the first available game client
                    GameClientInstallation? availableGameClientInstallation = GetFirstAvailableGameClient(gameInstallation);

                    // If one was found we attach it
                    if (availableGameClientInstallation != null)
                    {
                        await AttachGameClientImplAsync(gameInstallation, availableGameClientInstallation);
                    }
                    // Otherwise detach
                    else
                    {
                        Logger.Trace("Failed to attach a default game client for {0} due to one not being found",
                            gameInstallation.FullId);
                        await DetachGameClientImplAsync(gameInstallation);
                    }
                }
                else
                {
                    await DetachGameClientImplAsync(gameInstallation);
                }
            }
        }

        // Refresh all modified games in a single message
        if (modifiedGames.Any())
            Messenger.Send(new ModifiedGamesMessage(modifiedGames, rebuiltComponents: true));

        Logger.Info("The game client {0} has been removed", gameClientInstallation.FullId);
    }

    private async Task DetachGameClientImplAsync(GameInstallation gameInstallation)
    {
        // Get the previous client installation and invoke it being detached
        GameClientInstallation? prevClient = GetAttachedGameClient(gameInstallation);
        if (prevClient != null)
            await gameInstallation.GetComponents<OnGameClientDetachedComponent>().InvokeAllAsync(prevClient);

        // Detach the client for the game
        gameInstallation.SetValue<string?>(GameDataKey.Client_AttachedClient, null);

        // Rebuild the game components since the client change might change which components get registered
        gameInstallation.RebuildComponents();

        Logger.Info("Detached game client {0} from {1}", prevClient?.FullId, gameInstallation.FullId);
    }

    private async Task AttachGameClientImplAsync(GameInstallation gameInstallation, GameClientInstallation gameClientInstallation)
    {
        if (gameInstallation == null)
            throw new ArgumentNullException(nameof(gameInstallation));
        if (gameClientInstallation == null)
            throw new ArgumentNullException(nameof(gameClientInstallation));

        // Get the previous client installation and invoke it being detached
        GameClientInstallation? prevClient = GetAttachedGameClient(gameInstallation);

        // If the previous one is the same as what we're trying to attach then we return
        if (gameClientInstallation == prevClient)
        {
            Logger.Info("Cancelling attaching game client {0} to {1} due to it already being attached", gameClientInstallation.FullId, gameInstallation.FullId);
            return;
        }

        // Invoke the previous one being detached
        if (prevClient != null)
            await gameInstallation.GetComponents<OnGameClientDetachedComponent>().InvokeAllAsync(prevClient);

        // Set the client for the game
        gameInstallation.SetValue(GameDataKey.Client_AttachedClient, gameClientInstallation.InstallationId);

        // Rebuild the game components since the client change might change which components get registered
        gameInstallation.RebuildComponents();

        Logger.Info("Attached the game client {0} to {1}", gameClientInstallation.FullId, gameInstallation.FullId);

        // Invoke the new client being selected
        await gameInstallation.GetComponents<OnGameClientAttachedComponent>().InvokeAllAsync(gameClientInstallation);
    }

    #endregion

    #region Game Client Descriptor Methods

    /// <summary>
    /// Gets the available game client descriptors
    /// </summary>
    /// <returns>The game client descriptors</returns>
    public IReadOnlyList<GameClientDescriptor> GetGameClientDescriptors() => SortedGameClientDescriptors;

    /// <summary>
    /// Gets a game client descriptor from the id
    /// </summary>
    /// <param name="gameClientId">The game client id</param>
    /// <returns>The matching game client descriptor</returns>
    public GameClientDescriptor GetGameClientDescriptor(string gameClientId)
    {
        if (gameClientId == null)
            throw new ArgumentNullException(nameof(gameClientId));

        if (!GameClientDescriptors.TryGetValue(gameClientId, out GameClientDescriptor descriptor))
            throw new ArgumentException($"No game client descriptor found for the provided game client id {gameClientId}", nameof(gameClientId));

        return descriptor;
    }

    /// <summary>
    /// Gets a game client descriptor from the type
    /// </summary>
    /// <returns>The matching game client descriptor</returns>
    public T GetGameClientDescriptor<T>()
        where T : GameClientDescriptor
    {
        foreach (GameClientDescriptor clientDescriptor in SortedGameClientDescriptors)
        {
            if (clientDescriptor is T c)
                return c;
        }

        throw new Exception($"No game client descriptor found for the provided type {typeof(T)}");
    }

    #endregion

    #region Game Client Installation Methods

    public async Task<GameClientInstallation> AddGameClientAsync(GameClientToAdd gameClientToAdd)
    {
        return await AddGameClientAsync(
            descriptor: gameClientToAdd.GameClientDescriptor, 
            installLocation: gameClientToAdd.InstallLocation,
            configureInstallation: gameClientToAdd.ConfigureInstallation);
    }

    public async Task<GameClientInstallation> AddGameClientAsync(
        GameClientDescriptor descriptor,
        InstallLocation installLocation,
        ConfigureGameClientInstallation? configureInstallation = null)
    {
        // Add the game client
        GameClientInstallation gameClientInstallation = await AddGameClientImplAsync(descriptor, installLocation, configureInstallation);

        // Send a message that it's been added
        Messenger.Send(new AddedGameClientsMessage(gameClientInstallation));

        return gameClientInstallation;
    }

    public async Task<IList<GameClientInstallation>> AddGameClientsAsync(IEnumerable<GameClientToAdd> gameClients)
    {
        List<GameClientInstallation> gameClientInstallations = new();

        // Add each game client
        foreach (GameClientToAdd gameClient in gameClients)
            gameClientInstallations.Add(await AddGameClientImplAsync(gameClient.GameClientDescriptor, gameClient.InstallLocation, gameClient.ConfigureInstallation));

        if (gameClientInstallations.Any())
            Messenger.Send(new AddedGameClientsMessage(gameClientInstallations));

        return gameClientInstallations;
    }

    public async Task RemoveGameClientAsync(GameClientInstallation gameClientInstallation)
    {
        await RemoveGameClientImplAsync(gameClientInstallation);

        Messenger.Send(new RemovedGameClientsMessage(gameClientInstallation));
    }

    public Task RemoveGameClientAsync(string installationId)
    {
        GameClientInstallation? gameClientInstallation = GetInstalledGameClient(installationId);

        if (gameClientInstallation == null)
        {
            Logger.Info("The game client with the installation id {0} was not removed due to not being found", installationId);
            return Task.CompletedTask;
        }

        return RemoveGameClientAsync(gameClientInstallation);
    }

    public async Task RemoveGameClientsAsync(IList<GameClientInstallation> gameClientInstallations)
    {
        if (!gameClientInstallations.Any())
            return;

        // Remove the game clients
        foreach (GameClientInstallation gameClientInstallation in gameClientInstallations)
            await RemoveGameClientImplAsync(gameClientInstallation);

        Messenger.Send(new RemovedGameClientsMessage(gameClientInstallations));
    }

    public void SortGameClients(Comparison<GameClientInstallation> comparison)
    {
        Data.Game_GameClientInstallations.Sort(comparison);
        Messenger.Send(new SortedGameClientsMessage(Data.Game_GameClientInstallations.ToList()));
    }

    public void MoveGameClient(GameClientInstallation gameClient, int newIndex) =>
        MoveGameClient(Data.Game_GameClientInstallations.IndexOf(gameClient), newIndex);

    public void MoveGameClient(int srcIndex, int newIndex)
    {
        GameClientInstallation gameClient = Data.Game_GameClientInstallations[srcIndex];
        Data.Game_GameClientInstallations.RemoveAt(srcIndex);
        Data.Game_GameClientInstallations.Insert(newIndex, gameClient);

        Messenger.Send(new SortedGameClientsMessage(Data.Game_GameClientInstallations.ToList()));
    }

    /// <summary>
    /// Gets a collection of the currently installed game clients
    /// </summary>
    /// <returns>The game client installations</returns>
    public IReadOnlyList<GameClientInstallation> GetInstalledGameClients()
    {
        // Copy to a list to avoid issues with it being modified when enumerating
        return Data.Game_GameClientInstallations.ToList();
    }

    /// <summary>
    /// Gets a game client installation from the installation id
    /// </summary>
    /// <param name="installationId">The game client installation id</param>
    /// <returns>The matching game client installation or null if not found</returns>
    public GameClientInstallation? GetInstalledGameClient(string installationId)
    {
        if (installationId == null)
            throw new ArgumentNullException(nameof(installationId));

        return Data.Game_GameClientInstallations.FirstOrDefault(x => x.InstallationId == installationId);
    }

    /// <summary>
    /// Determines whether there are any installed game clients
    /// </summary>
    /// <returns>True if there is at least one installed game client, otherwise false</returns>
    public bool AnyInstalledGameClients() => 
        Data.Game_GameClientInstallations.Any();

    /// <summary>
    /// Determines whether there are any installed game clients which meet the specified criteria
    /// </summary>
    /// <returns>True if there is at least one installed game client which meets the specified criteria, otherwise false</returns>
    public bool AnyInstalledGameClients(Func<GameClientInstallation, bool> predicate) => 
        Data.Game_GameClientInstallations.Any(predicate);

    /// <summary>
    /// Gets finder items for the game clients which have not yet been added
    /// </summary>
    /// <returns>The finder items</returns>
    public IEnumerable<FinderItem> GetFinderItems()
    {
        // Get the currently installed game clients
        IReadOnlyList<GameClientInstallation> installedGameClients = GetInstalledGameClients();

        // Get finder items for all game clients which don't have an added game client installation
        foreach (GameClientDescriptor gameClientDescriptor in GetGameClientDescriptors())
        {
            // Make sure the game client has not already been added
            if (installedGameClients.Any(g => g.GameClientDescriptor == gameClientDescriptor))
                continue;

            // Get the finder item for the game client
            GameClientFinderItem? finderItem = gameClientDescriptor.GetFinderItem();

            if (finderItem == null)
                continue;

            yield return finderItem;
        }
    }

    #endregion

    #region Game Methods

    /// <summary>
    /// Gets the game client installation attached to the game installation, or null if there is none
    /// </summary>
    /// <param name="gameInstallation">The game installation to get the attached client for</param>
    /// <returns>The attached client installation or null if none was found</returns>
    public GameClientInstallation? GetAttachedGameClient(GameInstallation gameInstallation)
    {
        string? clientInstallationId = gameInstallation.GetValue<string>(GameDataKey.Client_AttachedClient);

        if (clientInstallationId == null)
            return null;

        return GetInstalledGameClient(clientInstallationId);
    }

    /// <summary>
    /// Gets the game client installation attached to the game installation
    /// </summary>
    /// <param name="gameInstallation">The game installation to get the attached client for</param>
    /// <returns>The attached client installation</returns>
    public GameClientInstallation GetRequiredAttachedGameClient(GameInstallation gameInstallation) =>
        GetAttachedGameClient(gameInstallation) ?? throw new Exception("The game does not have an attached game client");

    /// <summary>
    /// Detaches any currently attached game client from the game installation
    /// </summary>
    /// <param name="gameInstallation">The game installation to detach the game client for</param>
    public async Task DetachGameClientAsync(GameInstallation gameInstallation)
    {
        // Detach
        await DetachGameClientImplAsync(gameInstallation);

        // Refresh the game
        Messenger.Send(new ModifiedGamesMessage(gameInstallation, rebuiltComponents: true));
    }

    /// <summary>
    /// Gets the first available game client for a game installation or null if none is found
    /// </summary>
    /// <param name="gameInstallation">The game installation to get the game client for</param>
    /// <returns>The first available game client or null if none was found</returns>
    public GameClientInstallation? GetFirstAvailableGameClient(GameInstallation gameInstallation)
    {
        // Get the first available game client which does not require account verification to run the game
        return GetInstalledGameClients().FirstOrDefault(x => x.GameClientDescriptor.SupportsGame(gameInstallation, x) &&
                                                             !x.GameClientDescriptor.RequiresAccountVerification);
    }

    /// <summary>
    /// Attaches the specified game client to the game installation
    /// </summary>
    /// <param name="gameInstallation">The game installation to attach the game client to</param>
    /// <param name="gameClientInstallation">The game client to attach</param>
    public async Task AttachGameClientAsync(GameInstallation gameInstallation, GameClientInstallation gameClientInstallation)
    {
        // Attach the client
        await AttachGameClientImplAsync(gameInstallation, gameClientInstallation);

        // Refresh the game
        Messenger.Send(new ModifiedGamesMessage(gameInstallation, rebuiltComponents: true));
    }

    #endregion

    #region Records

    public record GameClientToAdd(
        GameClientDescriptor GameClientDescriptor,
        InstallLocation InstallLocation,
        ConfigureGameClientInstallation? ConfigureInstallation = null);

    #endregion
}