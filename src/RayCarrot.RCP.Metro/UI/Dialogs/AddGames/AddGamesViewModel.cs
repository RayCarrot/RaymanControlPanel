using System.IO;
using System.Text;
using System.Windows.Input;
using RayCarrot.RCP.Metro.Games.Finder;
using RayCarrot.RCP.Metro.Games.GameFileFinder;
using RayCarrot.RCP.Metro.Games.Structure;

namespace RayCarrot.RCP.Metro;

public class AddGamesViewModel : BaseViewModel, IInitializable, 
    IRecipient<AddedGamesMessage>, IRecipient<RemovedGamesMessage>
{
    #region Constructor

    public AddGamesViewModel()
    {
        GameCategories = new ObservableCollection<AddGamesGameCategoryViewModel>();

        // Enumerate every category of games
        foreach (var categorizedGames in Services.Games.GetGameDescriptors().GroupBy(x => x.Category))
        {
            // Create a view model
            GameCategoryInfoAttribute categoryInfo = categorizedGames.Key.GetInfo();
            AddGamesGameCategoryViewModel category = new(categoryInfo.DisplayName, categoryInfo.Icon);
            GameCategories.Add(category);

            // Enumerate every group of games
            foreach (var gameDescriptors in categorizedGames.GroupBy(x => x.Game))
            {
                // Get the game info
                GameInfoAttribute gameInfo = gameDescriptors.Key.GetInfo();

                // Add the group of games
                category.GameGroups.Add(new AddGamesGameGroupViewModel(
                    icon: gameInfo.GameIcon,
                    displayName: gameInfo.DisplayName,
                    gameDescriptors: gameDescriptors));
            }
        }

        FindGamesCommand = new AsyncRelayCommand(FindGamesAsync);
        FindGameFilesCommand = new AsyncRelayCommand(FindGameFilesAsync);
    }

    #endregion

    #region Logger

    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    #endregion

    #region Commands

    public ICommand FindGamesCommand { get; }
    public ICommand FindGameFilesCommand { get; }

    #endregion

    #region Public Properties

    public ObservableCollection<AddGamesGameCategoryViewModel> GameCategories { get; }
    public bool ShowGameFeatures { get; set; }

    public bool IsGameFinderRunning { get; set; }
    public bool IsGameFileFinderRunning { get; set; }

    #endregion

    #region Private Methods

    private void RefreshGames()
    {
        foreach (AddGamesGameCategoryViewModel gameCategory in GameCategories)
        {
            foreach (AddGamesGameGroupViewModel gameGroup in gameCategory.GameGroups)
            {
                foreach (AddGamesGameViewModel game in gameGroup.Games)
                {
                    game.Refresh();
                }
            }
        }
    }

    #endregion

    #region Public Methods

    public async Task FindGamesAsync()
    {
        if (IsGameFinderRunning)
            return;

        IsGameFinderRunning = true;

        FinderItem[] runFinderItems;

        try
        {
            // Create a finder
            Finder finder = new(Finder.DefaultOperations, Services.Games.GetFinderItems().ToArray());

            // Run the finder
            await finder.RunAsync();

            // Get the finder items
            runFinderItems = finder.FinderItems;
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Running finder");
            await Services.MessageUI.DisplayExceptionMessageAsync(ex, Resources.Finder_Error);
            return;
        }
        finally
        {
            IsGameFinderRunning = false;
        }

        // Get the games to add
        IEnumerable<GamesManager.GameToAdd> gamesToAdd = runFinderItems.
            OfType<GameFinderItem>().
            Select(x => x.GetGameToAdd()).
            Where(x => x != null)!;

        // Add the found games
        IList<GameInstallation> addedGames = await Services.Games.AddGamesAsync(gamesToAdd);

        if (addedGames.Any())
        {
            Logger.Info("The finder found {0} games", addedGames.Count);

            await Services.MessageUI.DisplayMessageAsync($"{Resources.GameFinder_GamesFound}{Environment.NewLine}{Environment.NewLine}• {addedGames.Select(x => x.GetDisplayName()).JoinItems(Environment.NewLine + "• ")}", Resources.GameFinder_GamesFoundHeader, MessageType.Success);
        }
        else
        {
            await Services.MessageUI.DisplayMessageAsync(Resources.Finder_NoGameResults, Resources.Finder_ResultHeader, MessageType.Information);
        }
    }

    public async Task FindGameFilesAsync()
    {
        if (IsGameFileFinderRunning)
            return;

        DirectoryBrowserResult browseResult = await Services.BrowseUI.BrowseDirectoryAsync(new DirectoryBrowserViewModel
        {
            Title = Resources.GameFileFinder_BrowseDirectoryHeader,
        });

        if (browseResult.CanceledByUser)
            return;

        IsGameFileFinderRunning = true;

        // Don't find games from the same paths. But we still allow the same game to be found multiple times.
        List<FileSystemPath> excludedPaths = Services.Games.GetInstalledGames().
            Where(x => x.GameDescriptor.Structure is SingleFileProgramInstallationStructure { SupportGameFileFinder: true }).
            Select(x => x.InstallLocation.FilePath).
            ToList();

        // Search for every game which is a single file
        List<GameFileFinderItem> finderItems = Services.Games.GetGameDescriptors().
            Where(x => x.Structure is SingleFileProgramInstallationStructure { SupportGameFileFinder: true }).
            Select(x => new GameFileFinderItem(x, (SingleFileProgramInstallationStructure)x.Structure)).
            ToList();

        try
        {
            GameFileFinder finder = new(browseResult.SelectedDirectory, SearchOption.AllDirectories, excludedPaths, finderItems);

            // Run the finder
            await Task.Run(finder.Run);
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Running game file finder");
            await Services.MessageUI.DisplayExceptionMessageAsync(ex, Resources.GameFileFinder_Error);
            return;
        }
        finally
        {
            IsGameFileFinderRunning = false;
        }

        // Get the games to add
        List<GamesManager.GameToAdd> gamesToAdd = new();
        foreach (GameFileFinderItem finderItem in finderItems)
        {
            foreach (InstallLocation installLocation in finderItem.GetFoundLocations())
            {
                gamesToAdd.Add(new GamesManager.GameToAdd(
                    GameDescriptor: finderItem.GameDescriptor,
                    InstallLocation: installLocation,
                    ConfigureInstallation: new ConfigureGameInstallation(
                        x =>
                        {
                            // Default the name to the filename
                            x.SetValue(GameDataKey.RCP_CustomName, x.InstallLocation.FilePath.RemoveFileExtension().Name);
                        })));
            }
        }

        // Add the found games
        IList<GameInstallation> addedGames = await Services.Games.AddGamesAsync(gamesToAdd);

        if (addedGames.Any())
        {
            Logger.Info("The game file finder found {0} games", addedGames.Count);

            StringBuilder sb = new();
            sb.Append(Resources.GameFileFinder_GamesFound);
            sb.AppendLine();

            for (int i = 0; i < addedGames.Count; i++)
            {
                sb.AppendLine();

                // Limit at 25
                if (i >= 25)
                {
                    sb.Append("...");
                    break;
                }

                sb.Append($"• {addedGames[i].GetDisplayName()}");
            }

            await Services.MessageUI.DisplayMessageAsync(sb.ToString(), Resources.GameFileFinder_GamesFoundHeader, MessageType.Success);
        }
        else
        {
            await Services.MessageUI.DisplayMessageAsync(Resources.GameFileFinder_NoResults, Resources.GameFileFinder_ResultHeader, MessageType.Information);
        }
    }

    public void Initialize() => Services.Messenger.RegisterAll(this);
    public void Deinitialize() => Services.Messenger.UnregisterAll(this);

    #endregion

    #region Message Receivers

    void IRecipient<AddedGamesMessage>.Receive(AddedGamesMessage message) => RefreshGames();
    void IRecipient<RemovedGamesMessage>.Receive(RemovedGamesMessage message) => RefreshGames();

    #endregion
}