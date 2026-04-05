using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Windows.Controls.Primitives;
using MahApps.Metro.Controls;
using RayCarrot.RCP.Metro.Games.Clients;
using RayCarrot.RCP.Metro.Games.Components;
using RayCarrot.RCP.Metro.Games.Finder;

namespace RayCarrot.RCP.Metro;

/// <summary>
/// Service for managing the application startup
/// </summary>
public class StartupManager
{
    #region Constructor

    public StartupManager(
        LaunchArguments args, 
        LoggerManager loggerManager,
        AppDataManager appDataManager,
        AppUserData data, 
        AppViewModel appViewModel, 
        JumpListManager jumpListManager, 
        DeployableFilesManager deployableFilesManager, 
        GamesManager gamesManager, 
        GameClientsManager gameClientsManager, 
        IMessageUIManager messageUi, 
        AppUIManager ui,
        RunningGamesManager runningGamesManager,
        DiscordManager discordManager)
    {
        Args = args ?? throw new ArgumentNullException(nameof(args));
        LoggerManager = loggerManager ?? throw new ArgumentNullException(nameof(loggerManager));
        AppDataManager = appDataManager ?? throw new ArgumentNullException(nameof(appDataManager));
        Data = data ?? throw new ArgumentNullException(nameof(data));
        AppViewModel = appViewModel ?? throw new ArgumentNullException(nameof(appViewModel));
        JumpListManager = jumpListManager ?? throw new ArgumentNullException(nameof(jumpListManager));
        DeployableFilesManager = deployableFilesManager ?? throw new ArgumentNullException(nameof(deployableFilesManager));
        GamesManager = gamesManager ?? throw new ArgumentNullException(nameof(gamesManager));
        GameClientsManager = gameClientsManager ?? throw new ArgumentNullException(nameof(gameClientsManager));
        MessageUI = messageUi ?? throw new ArgumentNullException(nameof(messageUi));
        UI = ui ?? throw new ArgumentNullException(nameof(ui));
        RunningGamesManager = runningGamesManager ?? throw new ArgumentNullException(nameof(runningGamesManager));
        DiscordManager = discordManager ?? throw new ArgumentNullException(nameof(discordManager));
    }

    #endregion

    #region Logger

    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    #endregion

    #region Services

    private LaunchArguments Args { get; }
    private LoggerManager LoggerManager { get; }
    private AppDataManager AppDataManager { get; }
    private AppUserData Data { get; }
    private AppViewModel AppViewModel { get; }
    private JumpListManager JumpListManager { get; }
    private DeployableFilesManager DeployableFilesManager { get; }
    private GamesManager GamesManager { get; }
    private GameClientsManager GameClientsManager { get; }
    private IMessageUIManager MessageUI { get; }
    private AppUIManager UI { get; }
    private RunningGamesManager RunningGamesManager { get; }
    private DiscordManager DiscordManager { get; }

    #endregion

    #region Private Properties

    private TimeSpan SplashScreenFadeoutTime => TimeSpan.FromMilliseconds(200);
    private SplashScreen? SplashScreen { get; set; }
    private const string SplashScreenResourceName = "Files/SplashScreen_512px.png";

    #endregion

    #region Private Methods

    private void CloseSplashScreen()
    {
        SplashScreen?.Close(SplashScreenFadeoutTime);
        SplashScreen = null;
    }

    private void InitEnvironment()
    {
        // Hard code the current directory to avoid any issues with the
        // application launching from other locations than its own
        string? assemblyPath = Assembly.GetEntryAssembly()?.Location;
        string? assemblyDir = Path.GetDirectoryName(assemblyPath);

        if (assemblyDir != null)
            Directory.SetCurrentDirectory(assemblyDir);
    }

    private void InitLogging()
    {
        LoggerManager.Initialize(Args);

        // Log the current environment
        try
        {
            Logger.Info("Current platform: {0}", Environment.OSVersion.VersionString);
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Logging environment details");
        }
    }

    private void LoadAppData()
    {
        AppDataManager.Load();

        Logger.Info("Current version is {0}", AppViewModel.CurrentAppVersion);

        // Set the previous app version
        AppViewModel.PrevAppVersion = Data.App_LastVersion;
    }

    private void InitXAML()
    {
        // Set the theme
        App.Current.SetTheme(Data.Theme_DarkMode, Data.Theme_SyncTheme, Data.Theme_Color);
    }

    private void InitLocalization()
    {
        LocalizationManager.SetCulture(Data.App_CurrentCulture);

        // TODO: This doesn't seem to work if another window shows before the main one?
        // Set it again once we leave the async context
        // https://stackoverflow.com/questions/70198716/setting-culture-in-net5-wpf-application/70201743#70201743
        App.Current.Dispatcher.InvokeAsync(() => LocalizationManager.SetCulture(Data.App_CurrentCulture));
    }

    private void InitJumpList()
    {
        JumpListManager.Initialize();
    }

    private void CheckLaunchArgs()
    {
        Logger.Trace("Launch arguments:");
        foreach (string arg in Args.Args)
            Logger.Trace(arg);

        // Check for user level argument
        if (Args.HasArg("-ul", out string? ul))
        {
            try
            {
                Data.App_UserLevel = Enum.Parse(typeof(UserLevel), ul, true).CastTo<UserLevel>();
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Setting user level from args");
            }
        }

        // NOTE: Starting with the updater 3.0.0 (available from 4.5.0) this is no longer used.
        //       It must however be maintained for legacy support (i.e. updating to version 4.5.0+
        //       using an updater below 3.0.0)
        // Check for updater install argument
        if (Args.HasArg("-install", out string? updateFile))
        {
            try
            {
                if (File.Exists(updateFile))
                {
                    new FileSystemPath(updateFile).DeleteFile();
                    Logger.Info("The updater was deleted");
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Deleting updater");
            }
        }
    }

    private FileLaunchHandler? CheckFileLaunch()
    {
        // Check if the app was opened from a file
        if (Args.FilePathArg != null)
            // Attempt to validate with an available file launch handler
            return FileLaunchHandler.GetHandler(Args.FilePathArg.Value);

        return null;
    }

    private UriLaunchHandler? CheckURILaunch()
    {
        // Check if the app was opened from a URI
        if (Args.HasArgs)
            return UriLaunchHandler.GetHandler(Args.Args[0]);

        return null;
    }

    private void UpdateAppLocation() // NOTE: This has to run before the post-update
    {
        string? assemblyPath = Assembly.GetEntryAssembly()?.Location;

        // Log some debug information
        Logger.Debug("Entry assembly path: {0}", assemblyPath);

        // Update the application path
        if (assemblyPath == Data.App_ApplicationPath) 
            return;
        
        Data.App_ApplicationPath = assemblyPath;
        Logger.Info("The application path has been updated");

        // Refresh the jump-list
        JumpListManager.Refresh();

        if (!File.Exists(assemblyPath)) 
            return;
        
        // Update file type and uri associations
        foreach (FileLaunchHandler handler in FileLaunchHandler.GetHandlers())
        {
            if (handler.IsAssociatedWithFileType() != true)
                continue;

            try
            {
                handler.AssociateWithFileType(assemblyPath, true);
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Setting file type association");
            }
        }

        foreach (UriLaunchHandler handler in UriLaunchHandler.GetHandlers())
        {
            if (handler.IsAssociatedWithUriProtocol() != true) 
                continue;
            
            try
            {
                handler.AssociateWithUriProtocol(assemblyPath, true);
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Setting uri protocol association");
            }
        }
    }

    private void CheckFirstLaunch()
    {
        // Show first launch info
        if (Data.App_IsFirstLaunch ||
            // Show if updated to version 12 due to it having been changed
            Data.App_LastVersion < new Version(12, 0, 0, 5))
        {
            // Close the splash screen
            CloseSplashScreen();

            // Show the first launch dialog
            new FirstLaunchInfoDialog().ShowDialog();

            Data.App_IsFirstLaunch = false;
        }
    }

    private async Task ValidateGamesAsync()
    {
        // Keep track of removed games
        List<GameInstallation> removed = new();

        // Make sure every game is valid
        foreach (GameInstallation gameInstallation in GamesManager.GetInstalledGames())
        {
            // Check if it's valid
            if (gameInstallation.GameDescriptor.ValidateLocation(gameInstallation.InstallLocation, GameValidationFlags.Location).IsValid &&
                gameInstallation.GetComponents<GameValidationCheckComponent>().All(x => x.IsValid()))
                continue;

            // Add to removed games
            removed.Add(gameInstallation);

            Logger.Info("The game {0} is being removed due to not being valid", gameInstallation.FullId);
        }

        // Return if no games were removed
        if (!removed.Any())
            return;

        // Remove the games
        await GamesManager.RemoveGamesAsync(removed);

        await MessageUI.DisplayMessageAsync(String.Format(Resources.Games_RemovedInvalidGames, 
            String.Join(Environment.NewLine, removed.Select(x => x.GetDisplayName()))), Resources.Games_RemovedInvalidGamesHeader, MessageType.Error);
    }

    private async Task ValidateGameClientsAsync()
    {
        // Keep track of removed game clients
        List<GameClientInstallation> removed = new();

        // Make sure every game client is valid
        foreach (GameClientInstallation gameClientInstallation in GameClientsManager.GetInstalledGameClients())
        {
            // Check if it's valid
            if (gameClientInstallation.GameClientDescriptor.IsValid(gameClientInstallation.InstallLocation))
                continue;

            // Add to removed game clients
            removed.Add(gameClientInstallation);

            Logger.Info("The game client {0} is being removed due to not being valid", gameClientInstallation.FullId);
        }

        // Return if no game clients were removed
        if (!removed.Any())
            return;

        // Remove the game clients
        await GameClientsManager.RemoveGameClientsAsync(removed);

        await MessageUI.DisplayMessageAsync(String.Format(Resources.GameClients_RemovedInvalidClients, 
            String.Join(Environment.NewLine, removed.Select(x => x.GetDisplayName()))), Resources.GameClients_RemovedInvalidClientsHeader, MessageType.Error);
    }

    private async Task PostUpdateAsync()
    {
        // Check if it's a new version
        if (Data.App_LastVersion < AppViewModel.CurrentAppVersion)
        {
            await AppDataManager.PostUpdateAsync(Data.App_LastVersion);

            // Refresh the jump list
            JumpListManager.Refresh();

            // Update the last version
            Data.App_LastVersion = AppViewModel.CurrentAppVersion;

            // Save the app data
            AppDataManager.Save();

            try
            {
                // Clear the temp folder
                AppFilePaths.TempPath.DeleteDirectory();
            }
            catch (Exception ex)
            {
                Logger.Warn(ex, "Clearing app temp folder");
            }
        }
        // Check if it's a lower version than previously recorded
        else if (Data.App_LastVersion > AppViewModel.CurrentAppVersion)
        {
            Logger.Warn("A newer version ({0}) has been recorded in the application data", Data.App_LastVersion);

            if (!Data.Update_DisableDowngradeWarning)
                await MessageUI.DisplayMessageAsync(String.Format(Resources.DowngradeWarning, AppViewModel.CurrentAppVersion,
                    Data.App_LastVersion), Resources.DowngradeWarningHeader, MessageType.Warning);
        }
    }

    private void ShowAppWindow<AppWindow>(Func<AppWindow> createWindow, bool isFullStartup)
        where AppWindow : Window
    {
        // Create the main window
        AppWindow appWindow = createWindow();

        // Load previous state
        Data.UI_WindowState?.ApplyToWindow(appWindow);

        appWindow.PreviewKeyDown += async (_, e) =>
        {
            if (e.OriginalSource is not TextBoxBase)
                await SecretCodeManager.AddKeyAsync(e.Key);
        };

        // Only perform the loaded actions if this is a full startup
        if (isFullStartup)
            appWindow.Loaded += AppWindow_OnLoaded;
        
        appWindow.Closing += AppWindow_OnClosing;
        appWindow.Closed += AppWindow_OnClosed;

        // Close the splash screen
        CloseSplashScreen();

        // Show the main window
        appWindow.Show();
    }

    private Task CleanDeployedFilesAsync()
    {
        // Clean up deployed files
        return Task.Run(DeployableFilesManager.CleanupFilesAsync);
    }

    private async Task ShowLogViewerAsync()
    {
        // Show log viewer if available
        if (LoggerManager.IsLogViewerAvailable)
        {
            await App.Current.Dispatcher.InvokeAsync(() => LogViewer.Open(LoggerManager.LogViewerViewModel));
            App.Current.MainWindow?.Focus();
        }
    }

    private Task CheckForUpdatesAsync()
    {
        // Check for updates
        if (Data.Update_AutoUpdate)
            return Task.Run(() => AppViewModel.CheckForUpdatesAsync(false));
        else
            return Task.CompletedTask;
    }

    private async Task FindInstalledGamesAsync()
    {
        // Find installed games if set to do so on startup
        if (Data.Game_AutoLocateGames)
        {
            FinderItem[] runFinderItems;

            try
            {

                // Add the items for game clients and games
                List<FinderItem> finderItems = new();
                finderItems.AddRange(Services.GameClients.GetFinderItems());
                finderItems.AddRange(Services.Games.GetFinderItems());

                // Create a finder
                Finder finder = new(Finder.DefaultOperations, finderItems.ToArray());

                // Run the finder
                await finder.RunAsync();

                // Get the finder items
                runFinderItems = finder.FinderItems;
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Running finder on startup");
                await Services.MessageUI.DisplayExceptionMessageAsync(ex, Resources.Finder_Error);
                return;
            }

            // Get the game clients to add
            IEnumerable<GameClientsManager.GameClientToAdd> gameClientsToAdd = runFinderItems.
                OfType<GameClientFinderItem>().
                Select(x => x.GetGameClientToAdd()).
                Where(x => x != null)!;

            // Add the found game clients
            IList<GameClientInstallation> addedGameClients = await Services.GameClients.AddGameClientsAsync(gameClientsToAdd);

            if (addedGameClients.Any())
            {
                Logger.Info("The finder found {0} game clients", addedGameClients.Count);

                await Services.MessageUI.DisplayMessageAsync($"{Resources.Finder_FoundClients}{Environment.NewLine}{Environment.NewLine}• {addedGameClients.Select(x => x.GetDisplayName()).JoinItems(Environment.NewLine + "• ")}", Resources.Finder_FoundClientsHeader, MessageType.Success);
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
        }
    }

    #endregion

    #region Event Handlers

    private async void AppWindow_OnLoaded(object sender, RoutedEventArgs e)
    {
        Logger.Info("Running startup operations after app window has loaded");

        // Only show update dialogs if this is not the first launch
        if (!AppViewModel.IsFirstLaunch)
        {
            // Show the anniversary update dialog if updated to the anniversary update (14.0)
            if (AppViewModel.PrevAppVersion < new Version(14, 0, 0, 5))
                await Services.UI.ShowAnniversaryUpdateAsync();

            // Show the version history if updated to a new version
            if (AppViewModel.PrevAppVersion < AppViewModel.CurrentAppVersion)
                await UI.ShowVersionHistoryAsync();
        }

        // Show April Fools' message if it's April Fools' Day
        if (DateTime.Now.Month == 4 && DateTime.Now.Day == 1 && Data.App_LastYearShownAprilFoolsMessage < DateTime.Now.Year)
        {
            await Services.MessageUI.DisplayMessageAsync(Resources.AprilFoolsMessage, Resources.AprilFoolsMessageHeader, MessageType.Information);
            Data.App_LastYearShownAprilFoolsMessage = DateTime.Now.Year;
        }

        // Clean deployed files
        await CleanDeployedFilesAsync();

        // Check for updates and installed games
        await Task.WhenAll(new[]
        {
            CheckForUpdatesAsync(),
            FindInstalledGamesAsync(),
        });
     
        Logger.Info("Finished running startup operations after app window has loaded");
    }

    private static async void AppWindow_OnClosing(object sender, CancelEventArgs e)
    {
        // NOTE: When the actual app shutdown is called this even will be invoked
        //       which will cause the below code to run twice if the user closes
        //       the window. However that doesn't matter since e.Cancel is ignored
        //       the second time and ShutdownAppAsync won't run while shutting down.

        // Cancel the native closing
        e.Cancel = true;

        // Don't close if the close button is disabled (such as from F4)
        if (sender is MetroWindow { IsCloseButtonEnabled: false })
            return;

        Logger.Info("The app window is closing...");

        // Shut down the app
        await App.Current.ShutdownAppAsync(false);
    }

    private static void AppWindow_OnClosed(object sender, EventArgs e)
    {
        // Make sure the app shuts down. It should already
        // be shutting down at this point in which case
        // this will be ignored.
        App.Current.Shutdown();
    }

    #endregion

    #region Public Methods

    public async Task RunAsync<AppWindow>(bool isFullStartup, Func<AppWindow> createWindow)
        where AppWindow : Window
    {
        try
        {
            Stopwatch sw = Stopwatch.StartNew();

            // Create the splash screen and show it
            SplashScreen = new SplashScreen(SplashScreenResourceName);
            SplashScreen.Show(false);

            AppViewModel.IsStartupRunning = true;

            InitEnvironment();
            InitLogging();
            Logger.Debug("Startup {0} ms: Initialized environment and logging", sw.ElapsedMilliseconds);

            LoadAppData();
            Logger.Debug("Startup {0} ms: Loaded app data", sw.ElapsedMilliseconds);

            InitXAML();
            InitLocalization();
            InitJumpList();
            Logger.Debug("Startup {0} ms: Initialized XAML, localization and jump list", sw.ElapsedMilliseconds);

            CheckLaunchArgs();
            FileLaunchHandler? fileLaunchHandler = CheckFileLaunch();
            UriLaunchHandler? uriLaunchHandler = CheckURILaunch();
            Logger.Debug("Startup {0} ms: Checked launch arguments and initialized web protocol", sw.ElapsedMilliseconds);

            UpdateAppLocation();
            Logger.Debug("Startup {0} ms: Updated app location", sw.ElapsedMilliseconds);

            // The file or URI launch can optionally disable the full startup
            if (fileLaunchHandler?.DisableFullStartup == true ||
                uriLaunchHandler?.DisableFullStartup == true)
                isFullStartup = false;

            if (isFullStartup)
            {
                CheckFirstLaunch();
                await ValidateGamesAsync();
                await ValidateGameClientsAsync();
                await PostUpdateAsync();
                Logger.Debug("Startup {0} ms: Checked first launch, validated games and clients & ran post-update", sw.ElapsedMilliseconds);
            }

            // Start checking for running games
            RunningGamesManager.Start();

            // Initialize the Discord manager
            DiscordManager.Initialize();

            ShowAppWindow<AppWindow>(createWindow, isFullStartup);
            Logger.Debug("Startup {0} ms: Showed app window", sw.ElapsedMilliseconds);

            // Show the log viewer if set to do so
            await ShowLogViewerAsync();
            Logger.Debug("Startup {0} ms: Optionally showed log viewer", sw.ElapsedMilliseconds);

            // Invoke the file or uri launch handler if we have one
            fileLaunchHandler?.Invoke(Args.FilePathArg!.Value, LaunchArgHandler.State.Startup);
            uriLaunchHandler?.Invoke(Args.Args[0], LaunchArgHandler.State.Startup);

            // Start receiving arguments from potentially new process
            Args.StartReceiveArguments();

            sw.Stop();

            Logger.Info("Finished running startup in {0} ms", sw.ElapsedMilliseconds);
        }
        finally
        {
            AppViewModel.IsStartupRunning = false;
            CloseSplashScreen();
        }
    }

    #endregion
}