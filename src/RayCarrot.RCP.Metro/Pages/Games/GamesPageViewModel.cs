using System.ComponentModel;
using System.Windows.Data;
using System.Windows.Input;
using Nito.AsyncEx;

namespace RayCarrot.RCP.Metro.Pages.Games;

/// <summary>
/// View model for the games page
/// </summary>
public class GamesPageViewModel : BasePageViewModel, 
    IRecipient<AddedGamesMessage>, IRecipient<RemovedGamesMessage>, IRecipient<ModifiedGamesMessage>,
    IRecipient<SortedGamesMessage>, IRecipient<ModifiedGameModsMessage>, IRecipient<FixedSetupGameActionMessage>,
    IRecipient<GameRunningChangedMessage>, IRecipient<SecretCodeEnteredMessage>
{
    #region Constructor

    public GamesPageViewModel(
        AppViewModel app,
        GamesManager gamesManager, 
        AppUIManager ui,
        IMessageUIManager messageUi,
        IMessenger messenger, 
        AppUserData data, 
        NewsViewModel newsViewModel) : base(app)
    {
        // Set services
        GamesManager = gamesManager ?? throw new ArgumentNullException(nameof(gamesManager));
        UI = ui ?? throw new ArgumentNullException(nameof(ui));
        MessageUI = messageUi ?? throw new ArgumentNullException(nameof(messageUi));
        Messenger = messenger ?? throw new ArgumentNullException(nameof(messenger));
        Data = data ?? throw new ArgumentNullException(nameof(data));
        NewsViewModel = newsViewModel ?? throw new ArgumentNullException(nameof(newsViewModel));

        // Set properties
        AsyncLock = new AsyncLock();
        GamesCategories = new ObservableCollection<GameCategoryViewModel>(
            EnumHelpers.GetValues<GameCategory>().
            Select(x => new GameCategoryViewModel(x)));

        // Set up the games collection
        Games = new ObservableCollectionEx<InstalledGameViewModel>();
        GamesView = CollectionViewSource.GetDefaultView(Games);
        GamesView.Filter = x => MatchesFilter((InstalledGameViewModel)x);

        // Create commands
        DeselectGameCommand = new RelayCommand(DeselectGame);
        SelectGameCommand = new RelayCommand(x => SelectGame((InstalledGameViewModel)x!));
        ResetSortCommand = new RelayCommand(ResetSort);
        RefreshGamesCommand = new AsyncRelayCommand(RefreshAsync);
        AddGamesCommand = new AsyncRelayCommand(AddGamesAsync);
        ConfigureGameClientsCommand = new AsyncRelayCommand(ConfigureGameClientsAsync);
    }

    #endregion

    #region Logger

    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    #endregion

    #region Commands

    public ICommand DeselectGameCommand { get; }
    public ICommand SelectGameCommand { get; }
    public ICommand ResetSortCommand { get; }
    public ICommand RefreshGamesCommand { get; }
    public ICommand AddGamesCommand { get; }
    public ICommand ConfigureGameClientsCommand { get; }

    #endregion

    #region Private Fields

    private string _gameFilter = String.Empty;
    private InstalledGameViewModel? _selectedInstalledGame;
    private bool _isSettingGroupSelection;
    private readonly Dictionary<Game, GameGroupViewModel> _gameGroups = new();

    #endregion

    #region Private Properties

    private AsyncLock AsyncLock { get; }

    #endregion

    #region Services

    private GamesManager GamesManager { get; }
    private AppUIManager UI { get; }
    private IMessageUIManager MessageUI { get; }
    private IMessenger Messenger { get; }
    private AppUserData Data { get; }

    #endregion

    #region Public Properties

    public override AppPage Page => AppPage.Games;

    public NewsViewModel NewsViewModel { get; }

    public string GameFilter
    {
        get => _gameFilter;
        set
        {
            _gameFilter = value;
            GamesView.Refresh();
        }
    }

    public ObservableCollection<GameCategoryViewModel> GamesCategories { get; }
    public ObservableCollectionEx<InstalledGameViewModel> Games { get; }
    public ICollectionView GamesView { get; }
    public ObservableCollectionEx<InstalledGameViewModel>? RecentGames { get; set; }
    public ObservableCollectionEx<InstalledGameViewModel>? FavoriteGames { get; set; }

    public bool ShowRecentGames { get; set; }
    public bool ShowRecentSetting
    {
        get => Data.UI_ShowRecentGames;
        set
        {
            Data.UI_ShowRecentGames = value;
            RefreshShowRecentGames();
        }
    }
    public bool ShowFavoriteGames { get; set; }
    public bool ShowAnyGames => ShowRecentGames || ShowFavoriteGames;

    public InstalledGameViewModel? SelectedInstalledGame
    {
        get => _selectedInstalledGame;
        set
        {
            _isSettingGroupSelection = true;

            try
            {
                if (value != null)
                {
                    foreach (GameGroupViewModel group in _gameGroups.Values)
                        group.IsSelected = value.GameGroup == group;
                }
                else
                {
                    foreach (GameGroupViewModel group in _gameGroups.Values)
                        group.IsSelected = false;
                }
            }
            finally
            {
                _isSettingGroupSelection = false;
            }

            SetSelectedGame(value);
        }
    }

    public bool GroupGames
    {
        get => Data.UI_GroupInstalledGames;
        set
        {
            Data.UI_GroupInstalledGames = value;

            // Sort the games to match the grouping. Otherwise the order
            // in the ui doesn't match the actual order and moving the
            // games around won't work as expected
            GamesManager.SortGames((x, y) =>
            {
                int categoryComparison = x.GameDescriptor.Category.CompareTo(y.GameDescriptor.Category);
                if (categoryComparison != 0)
                    return categoryComparison;

                return x.GameDescriptor.Game.CompareTo(y.GameDescriptor.Game);
            });
            
            RefreshGrouping(value);
        }
    }

    public bool ShowGameCategories => GroupGames && _gameGroups.Count > 5;

    #endregion

    #region Private Methods

    private bool MatchesFilter(InstalledGameViewModel game)
    {
        bool matchesString(string str) => str.IndexOf(GameFilter, StringComparison.CurrentCultureIgnoreCase) != -1;

        return matchesString(game.DisplayName) ||
               game.GameDescriptor.SearchKeywords.Any(matchesString);
    }

    private async void SetSelectedGame(InstalledGameViewModel? game)
    {
        _selectedInstalledGame = game;
        OnPropertyChanged(nameof(SelectedInstalledGame));

        if (game != null)
            await game.LoadAsync();
    }

    private void GameGroup_OnPropertyChanged(object sender, PropertyChangedEventArgs e)
    {
        if (_isSettingGroupSelection)
            return;

        // Handle selecting the group
        if (e.PropertyName == nameof(GameGroupViewModel.IsSelected) &&
            sender is GameGroupViewModel { IsSelected: true } group)
        {
            // Deselect all other groups
            foreach (GameGroupViewModel g in _gameGroups.Values.Where(x => x != group))
                g.IsSelected = false;

            // Select first game in group
            if (SelectedInstalledGame?.GameGroup != group)
                SetSelectedGame(Games.FirstOrDefault(x => x.GameGroup == group));
        }
    }

    private void RefreshGrouping(bool group)
    {
        if (group && GamesView.GroupDescriptions.Count == 0)
            GamesView.GroupDescriptions.Add(new PropertyGroupDescription(nameof(InstalledGameViewModel.GameGroup)));
        else
            GamesView.GroupDescriptions.Clear();
    }

    private void RefreshRecentGames()
    {
        RecentGames = new ObservableCollectionEx<InstalledGameViewModel>(Games.
            // Get the latest date (either last played or when the game was added)
            Select(x =>
            {
                DateTime lastPlayed = x.GameInstallation.GetValue<DateTime>(GameDataKey.RCP_LastPlayed);
                DateTime gameAdded = x.GameInstallation.GetValue<DateTime>(GameDataKey.RCP_GameAddedDate);

                return new { Game = x, Date = lastPlayed > gameAdded ? lastPlayed : gameAdded };
            }).
            // Only keep games from the last 100 days
            Where(x => DateTime.Now - x.Date <= TimeSpan.FromDays(100)).
            // Sort...
            OrderByDescending(x => x.Date).
            // Get the games
            Select(x => x.Game).
            // Keep max 10
            Take(10));

        RefreshShowRecentGames();
    }

    public void RefreshShowRecentGames()
    {
        ShowRecentGames = Data.UI_ShowRecentGames && RecentGames.Any();
    }

    private void RefreshFavoriteGames()
    {
        FavoriteGames = new ObservableCollectionEx<InstalledGameViewModel>(Games.
            Where(x => x.GameInstallation.GetValue<bool>(GameDataKey.RCP_IsFavorite)));

        ShowFavoriteGames = FavoriteGames.Any();
    }

    private void Game_OnLastPlayedChanged() => RefreshRecentGames();
    private void Game_OnIsFavoriteChanged() => RefreshFavoriteGames();

    #endregion

    #region Protected Methods

    protected override async Task InitializeAsync()
    {
        // Perform an initial refresh for the grouping
        RefreshGrouping(GroupGames);

        // Refresh the games
        await RefreshAsync();

        // Register for messages
        Messenger.RegisterAll(this);

        // Initialize the app news
        await NewsViewModel.InitializeAsync();
    }

    #endregion

    #region Public Methods

    public void DeselectGame() => SelectedInstalledGame = null;
    public void SelectGame(InstalledGameViewModel game) => SelectedInstalledGame = game;
    public void ResetSort() => GamesManager.SortGames((x, y) => x.CompareTo(y));

    public Task RefreshAsync() => RefreshAsync(null);

    public async Task RefreshAsync(GameInstallation? selectedGameInstallation)
    {
        using (await AsyncLock.LockAsync())
        {
            await Metro.App.Current.Dispatcher.Invoke(async () =>
            {
                try
                {
                    // Unload previously loaded games
                    foreach (InstalledGameViewModel game in Games)
                    {
                        game.GameInstallation.RemoveDataChangedCallback(GameDataKey.RCP_LastPlayed, Game_OnLastPlayedChanged);
                        game.GameInstallation.RemoveDataChangedCallback(GameDataKey.RCP_IsFavorite, Game_OnIsFavoriteChanged);
                        game.Unload();
                    }

                    foreach (GameGroupViewModel group in _gameGroups.Values)
                        group.PropertyChanged -= GameGroup_OnPropertyChanged;

                    foreach (GameCategoryViewModel category in GamesCategories)
                        category.IsEnabled = false;

                    SelectedInstalledGame = null;

                    Games.ModifyCollection(x =>
                    {
                        // Clear the games
                        x.Clear();
                        _gameGroups.Clear();

                        // Enumerate every group of installed games
                        foreach (GameInstallation gameInstallation in GamesManager.GetInstalledGames())
                        {
                            Game game = gameInstallation.GameDescriptor.Game;

                            if (!_gameGroups.TryGetValue(game, out GameGroupViewModel group))
                            {
                                // Create a view model for the group
                                GameInfoAttribute gameInfo = game.GetInfo();
                                group = new GameGroupViewModel(gameInfo.GameIcon, gameInfo.DisplayName);
                                group.PropertyChanged += GameGroup_OnPropertyChanged;

                                _gameGroups[game] = group;
                            }

                            GameCategoryViewModel category = GamesCategories.First(y => y.Category == gameInstallation.GameDescriptor.Category);

                            category.IsEnabled = true;

                            x.Add(new InstalledGameViewModel(gameInstallation, category, group));
                        }
                    });

                    // Game groups dictionary has been modified
                    OnPropertyChanged(nameof(ShowGameCategories));

                    RefreshRecentGames();
                    RefreshFavoriteGames();

                    foreach (InstalledGameViewModel game in Games)
                    {
                        game.GameInstallation.AddDataChangedCallback(GameDataKey.RCP_LastPlayed, Game_OnLastPlayedChanged);
                        game.GameInstallation.AddDataChangedCallback(GameDataKey.RCP_IsFavorite, Game_OnIsFavoriteChanged);
                    }

                    if (selectedGameInstallation != null)
                        SelectedInstalledGame = Games.FirstOrDefault(x => x.GameInstallation == selectedGameInstallation);
                }
                catch (Exception ex)
                {
                    Logger.Fatal(ex, "Refreshing games");

                    Games.Clear();
                    SelectedInstalledGame = null;
                    RecentGames = new ObservableCollectionEx<InstalledGameViewModel>();
                    FavoriteGames = new ObservableCollectionEx<InstalledGameViewModel>();

                    await MessageUI.DisplayExceptionMessageAsync(ex, Resources.GameSelection_RefreshError);
                }
                finally
                {
                    GamesView.Refresh();
                }
            });
        }
    }

    public async Task RefreshInstallations(IList<GameInstallation> gameInstallations, bool rebuiltComponents)
    {
        using (await AsyncLock.LockAsync())
        {
            foreach (InstalledGameViewModel installedGame in Games)
            {
                if (gameInstallations.Contains(installedGame.GameInstallation))
                {
                    await installedGame.RefreshAsync(rebuiltComponents);
                }
            }
        }
    }

    public Task AddGamesAsync() => UI.ShowAddGamesAsync();
    public Task ConfigureGameClientsAsync() => UI.ShowGameClientsSetupAsync();

    #endregion

    #region Message Receivers

    async void IRecipient<AddedGamesMessage>.Receive(AddedGamesMessage message) =>
        await RefreshAsync(message.GameInstallations.FirstOrDefault());
    async void IRecipient<RemovedGamesMessage>.Receive(RemovedGamesMessage message) =>
        await RefreshAsync(SelectedInstalledGame?.GameInstallation);
    async void IRecipient<ModifiedGamesMessage>.Receive(ModifiedGamesMessage message) =>
        await RefreshInstallations(message.GameInstallations, message.RebuiltComponents);
    void IRecipient<SortedGamesMessage>.Receive(SortedGamesMessage message)
    {
        Games.ModifyCollection(x =>
            x.Sort((x1, x2) => message.SortedCollection.
                IndexOf(x1.GameInstallation).
                CompareTo(message.SortedCollection.
                    IndexOf(x2.GameInstallation))));
        FavoriteGames?.ModifyCollection(x =>
            x.Sort((x1, x2) => message.SortedCollection.
                IndexOf(x1.GameInstallation).
                CompareTo(message.SortedCollection.
                    IndexOf(x2.GameInstallation))));
    }
    async void IRecipient<ModifiedGameModsMessage>.Receive(ModifiedGameModsMessage message)
    {
        using (await AsyncLock.LockAsync())
        {
            // Reload setup game actions whenever mods have been modified
            InstalledGameViewModel? installedGame = Games.FirstOrDefault(x => x.GameInstallation == message.GameInstallation);
            if (installedGame != null)
                await installedGame.SetupGameViewModel.LoadAsync();
        }
    }
    async void IRecipient<FixedSetupGameActionMessage>.Receive(FixedSetupGameActionMessage message)
    {
        using (await AsyncLock.LockAsync())
        {
            foreach (InstalledGameViewModel installedGame in Games)
            {
                if (message.GameInstallations.Contains(installedGame.GameInstallation))
                {
                    await installedGame.SetupGameViewModel.LoadAsync();
                }
            }
        }
    }
    async void IRecipient<GameRunningChangedMessage>.Receive(GameRunningChangedMessage message)
    {
        using (await AsyncLock.LockAsync())
        {
            InstalledGameViewModel? installedGame = Games.FirstOrDefault(x => x.GameInstallation == message.GameInstallation);
            installedGame?.IsRunning = message.IsRunning;
        }
    }

    async void IRecipient<SecretCodeEnteredMessage>.Receive(SecretCodeEnteredMessage message)
    {
        if (message.Code == "GUESTS")
        {
            using (await AsyncLock.LockAsync())
            {
                foreach (InstalledGameViewModel installedGame in Games)
                {
                    if (installedGame.GameDescriptor.Game == Game.RaymanLegends)
                    {
                        installedGame.GameBanner = GameBannerAsset.JacquouilleLegends;
                    }
                }
            }
        }
    }

    #endregion
}