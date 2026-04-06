using System.Diagnostics.CodeAnalysis;
using RayCarrot.RCP.Metro.Games.Components;
using RayCarrot.RCP.Metro.Games.Finder;

namespace RayCarrot.RCP.Metro;

public class GamesManager
{
    #region Constructor

    public GamesManager(AppUserData data, IMessenger messenger)
    {
        Data = data ?? throw new ArgumentNullException(nameof(data));
        Messenger = messenger ?? throw new ArgumentNullException(nameof(messenger));

        GameDescriptors = new GameDescriptor[]
        {
            // Rayman
            new GameDescriptor_Rayman1_MsDos(),
            new GameDescriptor_Rayman1_Jaguar(),
            new GameDescriptor_Rayman1_Ps1(),
            new GameDescriptor_Rayman1_Gba(),
            new GameDescriptor_Rayman30thAnniversaryEdition_Win32(),
            new GameDescriptor_RaymanDesigner_MsDos(),
            new GameDescriptor_RaymanByHisFans_MsDos(),
            new GameDescriptor_Rayman60Levels_MsDos(),
            new GameDescriptor_Rayman2_Win32(),
            new GameDescriptor_Rayman2_Ps1(),
            new GameDescriptor_Rayman2_Ps2(),
            new GameDescriptor_RaymanM_Win32(),
            new GameDescriptor_RaymanM_Ps2(),
            new GameDescriptor_RaymanArena_Win32(),
            new GameDescriptor_RaymanArena_Ps2(),
            new GameDescriptor_RaymanArena_GameCube(),
            new GameDescriptor_RaymanRush_Ps1(),
            new GameDescriptor_Rayman3_Win32(),
            new GameDescriptor_Rayman3_GameCube(),
            new GameDescriptor_Rayman3_Ps2(),
            new GameDescriptor_RaymanOrigins_Win32(),
            new GameDescriptor_RaymanLegends_Win32(),
            new GameDescriptor_RaymanJungleRun_WindowsPackage(),
            new GameDescriptor_RaymanJungleRun_Win32(),
            new GameDescriptor_RaymanFiestaRun_WindowsPackage(),
            new GameDescriptor_RaymanFiestaRun_PreloadEdition_WindowsPackage(),
            new GameDescriptor_RaymanFiestaRun_Windows10Edition_WindowsPackage(),
            new GameDescriptor_RaymanFiestaRun_Win32(),

            // Rabbids
            new GameDescriptor_RaymanRavingRabbids_Win32(),
            new GameDescriptor_RaymanRavingRabbids_Ps2(),
            new GameDescriptor_RaymanRavingRabbids2_Win32(),
            new GameDescriptor_RabbidsGoHome_Win32(),
            new GameDescriptor_RabbidsBigBang_WindowsPackage(),
            new GameDescriptor_RabbidsCoding_Win32(),

            // Handheld
            new GameDescriptor_Rayman1_Gbc(),
            new GameDescriptor_Rayman2_Gbc(),
            new GameDescriptor_Rayman3_Gba(),
            new GameDescriptor_Rayman3_Prototype_Gba(),
            new GameDescriptor_RaymanHoodlumsRevenge_Gba(),
            new GameDescriptor_RaymanRavingRabbids_Gba(),

            // Demos/prototypes (no longer a category in the app)
            new GameDescriptor_Rayman1_Prototype_Snes(),
            new GameDescriptor_Rayman1_Demo_19951207_MsDos(),
            new GameDescriptor_Rayman1_Demo_19960215_MsDos(),
            new GameDescriptor_Rayman1_Demo_19951204_MsDos(),
            new GameDescriptor_Rayman1_Prototype_Jaguar(),
            new GameDescriptor_Rayman1_Demo_Jaguar(),
            new GameDescriptor_RaymanGold_Demo_19970930_MsDos(),
            new GameDescriptor_Rayman2_Beta_19980722_Win32(),
            new GameDescriptor_Rayman2_Demo_19990818_Win32(),
            new GameDescriptor_Rayman2_Demo_19990904_Win32(),
            new GameDescriptor_RaymanM_Demo_20020627_Win32(),
            new GameDescriptor_Rayman3_Demo_20021004_Win32(),
            new GameDescriptor_Rayman3_Demo_20021021_Win32(),
            new GameDescriptor_Rayman3_Demo_20021210_Win32(),
            new GameDescriptor_Rayman3_Demo_20030129_Win32(),
            new GameDescriptor_Rayman3_Demo_20030108_Win32(),
            new GameDescriptor_RaymanRavingRabbids_Demo_20061106_Win32(),

            // Other
            new GameDescriptor_Rayman1Minigames_Win32(),
            new GameDescriptor_RaymanEdutainmentEdu_MsDos(),
            new GameDescriptor_RaymanEdutainmentEdu_Ps1(),
            new GameDescriptor_RaymanEdutainmentQuiz_MsDos(),
            new GameDescriptor_RaymanEdutainmentQuiz_Ps1(),
            new GameDescriptor_TonicTrouble_Win32(),
            new GameDescriptor_TonicTroubleSpecialEdition_Win32(),
            new GameDescriptor_TonicTrouble_Gbc(),
            new GameDescriptor_DonaldDuckQuackAttack_Win32(),
            new GameDescriptor_RaymanDictées_Win32(),
            new GameDescriptor_RaymanPremiersClics_Win32(),
            new GameDescriptor_Rayman3PrintStudio_Win32(),
            new GameDescriptor_RaymanActivityCenter_Win32(),
            new GameDescriptor_RaymanRavingRabbidsActivityCenter_Win32(),

            // Fan
            new GameDescriptor_TheDarkMagiciansReignofTerror_Win32(),
            new GameDescriptor_RaymanRedemption_Win32(),
            new GameDescriptor_RaymanRedesigner_Win32(),
            new GameDescriptor_RaymanBowling2_Win32(),
            new GameDescriptor_RaymanGardenPLUS_Win32(),
            new GameDescriptor_GloboxMoment_Win32(),
            new GameDescriptor_RaymanTheDreamersBoundary_Win32(),
            new GameDescriptor_RaymanLumoGrams_Demo_Win32(),
            new GameDescriptor_Rayman3HavocReignited_Win32(),
            new GameDescriptor_Rayman2Redreamed_Demo_Win32(),
            new GameDescriptor_RaymanMysteryOfPolokus_Win32(),
        }.ToDictionary(x => x.GameId);
        SortedGameDescriptors = GameDescriptors.Values.OrderBy(x => x).ToArray();
    }

    #endregion

    #region Logger

    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    #endregion

    #region Services

    private AppUserData Data { get; }
    private IMessenger Messenger { get; }

    #endregion

    #region Private Properties

    private Dictionary<string, GameDescriptor> GameDescriptors { get; }
    private GameDescriptor[] SortedGameDescriptors { get; }

    #endregion

    #region Private Methods

    private async Task<GameInstallation> AddGameImplAsync(
        GameDescriptor gameDescriptor,
        InstallLocation installLocation,
        ConfigureGameInstallation? configureInstallation = null)
    {
        // Create an installation
        GameInstallation gameInstallation = new(gameDescriptor, installLocation);

        // Build the components
        gameInstallation.RebuildComponents();

        // Add the game
        Data.Game_GameInstallations.AddSorted(gameInstallation);

        try
        {
            // Set the date it's being added
            gameInstallation.SetValue(GameDataKey.RCP_GameAddedDate, DateTime.Now);

            // Configure
            if (configureInstallation != null)
                await configureInstallation.Invoke(gameInstallation);

            Logger.Info("The game {0} has been added", gameInstallation.FullId);

            // Invoke added actions
            await gameInstallation.GetComponents<OnGameAddedComponent>().InvokeAllAsync();
        }
        catch
        {
            // Remove the game if there was an error during its initialization.
            // This is to avoid adding the game in an invalid state.
            Data.Game_GameInstallations.Remove(gameInstallation);
            throw;
        }

        return gameInstallation;
    }

    private async Task RemoveGameImplAsync(GameInstallation gameInstallation)
    {
        // Remove the game
        Data.Game_GameInstallations.Remove(gameInstallation);

        Logger.Info("The game {0} has been removed", gameInstallation.FullId);

        // Invoke removal actions
        await gameInstallation.GetComponents<OnGameRemovedComponent>().InvokeAllAsync();
    }

    #endregion

    #region Game Descriptor Methods

    /// <summary>
    /// Enumerates the available game descriptors
    /// </summary>
    /// <returns>The game descriptors</returns>
    public IReadOnlyList<GameDescriptor> GetGameDescriptors() => SortedGameDescriptors;

    /// <summary>
    /// Tried and gets a game descriptor from the id
    /// </summary>
    /// <param name="gameId">The game descriptor id</param>
    /// <param name="gameDescriptor">The game descriptor to return</param>
    /// <returns>True if a game descriptor was found</returns>
    public bool TryGetGameDescriptor(string gameId, [NotNullWhen(true)] out GameDescriptor? gameDescriptor)
    {
        if (gameId == null)
            throw new ArgumentNullException(nameof(gameId));

        return GameDescriptors.TryGetValue(gameId, out gameDescriptor);
    }

    /// <summary>
    /// Gets a game descriptor from the id
    /// </summary>
    /// <param name="gameId">The game descriptor id</param>
    /// <returns>The matching game descriptor</returns>
    public GameDescriptor GetGameDescriptor(string gameId)
    {
        if (gameId == null)
            throw new ArgumentNullException(nameof(gameId));

        if (!GameDescriptors.TryGetValue(gameId, out GameDescriptor descriptor))
            throw new ArgumentException($"No game descriptor found for the provided game id {gameId}", nameof(gameId));

        return descriptor;
    }

    /// <summary>
    /// Gets all of the game descriptors which match a legacy game id (a game id used prior to version 14.0)
    /// </summary>
    /// <param name="legacyGameId">The legacy game id</param>
    /// <returns>The matching game descriptors</returns>
    public IReadOnlyList<GameDescriptor> GetGameDescriptorsFromLegacyId(string legacyGameId)
    {
        if (legacyGameId == null) 
            throw new ArgumentNullException(nameof(legacyGameId));
        
        return SortedGameDescriptors.Where(x => x.LegacyGameId == legacyGameId).ToList();
    }

    #endregion

    #region Game Installation Methods

    public async Task<GameInstallation> AddGameAsync(GameToAdd gameToAdd)
    {
        return await AddGameAsync(gameToAdd.GameDescriptor, gameToAdd.InstallLocation, gameToAdd.ConfigureInstallation);
    }

    /// <summary>
    /// Adds a new game to the app
    /// </summary>
    /// <param name="gameDescriptor">The game descriptor for the game to add</param>
    /// <param name="installLocation">The game install location</param>
    /// <param name="configureInstallation">An optional action callback for configuring the added game installation</param>
    /// <returns>The game installation</returns>
    public async Task<GameInstallation> AddGameAsync(
        GameDescriptor gameDescriptor, 
        InstallLocation installLocation,
        ConfigureGameInstallation? configureInstallation = null)
    {
        // Add the game
        GameInstallation gameInstallation = await AddGameImplAsync(gameDescriptor, installLocation, configureInstallation);

        // Send a message that it's been added
        Messenger.Send(new AddedGamesMessage(gameInstallation));

        return gameInstallation;
    }

    /// <summary>
    /// Adds multiple new games to the app
    /// </summary>
    /// <param name="games">The games to add</param>
    /// <returns>The game installations</returns>
    public async Task<IList<GameInstallation>> AddGamesAsync(IEnumerable<GameToAdd> games)
    {
        List<GameInstallation> gameInstallations = new();

        // Add each game
        foreach (GameToAdd game in games)
            gameInstallations.Add(await AddGameImplAsync(game.GameDescriptor, game.InstallLocation, game.ConfigureInstallation));

        if (gameInstallations.Any())
            Messenger.Send(new AddedGamesMessage(gameInstallations));

        return gameInstallations;
    }

    /// <summary>
    /// Removes the specified game from the app
    /// </summary>
    /// <param name="gameInstallation">The game installation to remove</param>
    /// <returns>The task</returns>
    public async Task RemoveGameAsync(GameInstallation gameInstallation)
    {
        // Remove the game
        await RemoveGameImplAsync(gameInstallation);

        // Send a message that the game was removed
        Messenger.Send(new RemovedGamesMessage(gameInstallation));
    }

    /// <summary>
    /// Removes multiple games from the app
    /// </summary>
    /// <param name="gameInstallations">The games to remove</param>
    /// <returns>The task</returns>
    public async Task RemoveGamesAsync(IList<GameInstallation> gameInstallations)
    {
        if (!gameInstallations.Any())
            return;

        // Remove the games
        foreach (GameInstallation gameInstallation in gameInstallations)
            await RemoveGameImplAsync(gameInstallation);

        Messenger.Send(new RemovedGamesMessage(gameInstallations));
    }

    public void SortGames(Comparison<GameInstallation> comparison)
    {
        Data.Game_GameInstallations.Sort(comparison);
        Messenger.Send(new SortedGamesMessage(Data.Game_GameInstallations.ToList()));
    }

    public void MoveGame(GameInstallation game, int newIndex) =>
        MoveGame(Data.Game_GameInstallations.IndexOf(game), newIndex);

    public void MoveGame(int srcIndex, int newIndex)
    {
        GameInstallation game = Data.Game_GameInstallations[srcIndex];
        Data.Game_GameInstallations.RemoveAt(srcIndex);
        Data.Game_GameInstallations.Insert(newIndex, game);

        Messenger.Send(new SortedGamesMessage(Data.Game_GameInstallations.ToList()));
    }

    /// <summary>
    /// Gets a collection of the currently installed games
    /// </summary>
    /// <returns>The game installations</returns>
    public IReadOnlyList<GameInstallation> GetInstalledGames()
    {
        // Copy to a list to avoid issues with it being modified when enumerating
        return Data.Game_GameInstallations.ToList();
    }

    /// <summary>
    /// Determines whether there are any installed games
    /// </summary>
    /// <returns>True if there is at least one installed game, otherwise false</returns>
    public bool AnyInstalledGames() => Data.Game_GameInstallations.Any();

    /// <summary>
    /// Determines whether there are any installed games which meet the specified criteria
    /// </summary>
    /// <returns>True if there is at least one installed game which meets the specified criteria, otherwise false</returns>
    public bool AnyInstalledGames(Func<GameInstallation, bool> predicate) => Data.Game_GameInstallations.Any(predicate);

    /// <summary>
    /// Finds a game installation based on the provided search predicate
    /// </summary>
    /// <param name="gameSearchPredicate">The predicate to use when finding the game installation</param>
    /// <returns>The first matching game installation or null if none was found</returns>
    public GameInstallation? FindInstalledGame(GameSearch.Predicate gameSearchPredicate) =>
        Data.Game_GameInstallations.FirstOrDefault(x => gameSearchPredicate(x.GameDescriptor));

    /// <summary>
    /// Finds a game installation based on the provided search predicates
    /// </summary>
    /// <param name="gameSearchPredicates">The predicates to use when finding the game installation. A game is valid if at least one matches.</param>
    /// <returns>The first matching game installation or null if none was found</returns>
    public GameInstallation? FindInstalledGame(params GameSearch.Predicate[] gameSearchPredicates) =>
        FindInstalledGame(GameSearch.Create(gameSearchPredicates));

    /// <summary>
    /// Gets a game installation from the installation id
    /// </summary>
    /// <param name="installationId">The game installation id</param>
    /// <returns>The matching game installation or null if not found</returns>
    public GameInstallation? GetInstalledGame(string installationId)
    {
        if (installationId == null)
            throw new ArgumentNullException(nameof(installationId));

        return Data.Game_GameInstallations.FirstOrDefault(x => x.InstallationId == installationId);
    }

    /// <summary>
    /// Gets finder items for the games which have not yet been added
    /// </summary>
    /// <returns>The finder items</returns>
    public IEnumerable<FinderItem> GetFinderItems()
    {
        // Get the currently installed games
        IReadOnlyList<GameInstallation> installedGames = GetInstalledGames();

        // Get finder items for all games which don't have an added game installation
        foreach (GameDescriptor gameDescriptor in Services.Games.GetGameDescriptors())
        {
            // Make sure the game has not already been added
            if (installedGames.Any(g => g.GameDescriptor == gameDescriptor))
                continue;

            // Get the finder item for the game
            GameFinderItem? finderItem = gameDescriptor.GetFinderItem();

            if (finderItem == null)
                continue;

            yield return finderItem;
        }
    }

    #endregion

    #region Records

    public record GameToAdd(
        GameDescriptor GameDescriptor, 
        InstallLocation InstallLocation,
        ConfigureGameInstallation? ConfigureInstallation = null);

    #endregion
}