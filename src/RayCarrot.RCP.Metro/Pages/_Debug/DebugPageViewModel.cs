using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.Versioning;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using ControlzEx.Theming;
using RayCarrot.RCP.Metro.Games.Finder;

namespace RayCarrot.RCP.Metro.Pages.Debug;

/// <summary>
/// View model for the debug page
/// </summary>
public class DebugPageViewModel : BasePageViewModel
{
    #region Constructor

    /// <summary>
    /// Default constructor
    /// </summary>
    public DebugPageViewModel(
        AppViewModel app, 
        AppUserData data, 
        AppUIManager ui,
        FileManager fileManager, 
        IBrowseUIManager browseUi, 
        IMessageUIManager messageUi, 
        LoggerManager loggerManager, 
        AppDataManager appDataManager, 
        GamesManager gamesManager) : base(app)
    {
        // Set services
        Data = data ?? throw new ArgumentNullException(nameof(data));
        UI = ui ?? throw new ArgumentNullException(nameof(ui));
        FileManager = fileManager ?? throw new ArgumentNullException(nameof(fileManager));
        BrowseUI = browseUi ?? throw new ArgumentNullException(nameof(browseUi));
        MessageUI = messageUi ?? throw new ArgumentNullException(nameof(messageUi));
        LoggerManager = loggerManager ?? throw new ArgumentNullException(nameof(loggerManager));
        AppDataManager = appDataManager ?? throw new ArgumentNullException(nameof(appDataManager));
        GamesManager = gamesManager ?? throw new ArgumentNullException(nameof(gamesManager));

        // Create commands
        ShowDialogCommand = new AsyncRelayCommand(ShowDialogAsync);
        ShowLogCommand = new AsyncRelayCommand(ShowLogAsync);
        ShowWelcomeWindowCommand = new RelayCommand(ShowWelcomeWindow);
        RefreshDataOutputCommand = new RelayCommand(RefreshDataOutput);
        GCCollectCommand = new RelayCommand(GCCollect);
        ThrowUnhandledExceptionCommand = new RelayCommand(ThrowUnhandledException);
        ThrowUnhandledExceptionAsyncCommand = new AsyncRelayCommand(ThrowUnhandledAsyncException);
        RunInstallerCommand = new AsyncRelayCommand(RunInstallerAsync);
        ShutdownAppCommand = new AsyncRelayCommand(async () => await Task.Run(async () => await Metro.App.Current.ShutdownAppAsync(false)));
        UpdateThemeCommand = new RelayCommand(UpdateTheme);
        RunLoadOperationCommand = new AsyncRelayCommand(RunLoadOperationAsync);
    }

    #endregion

    #region Logger

    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    #endregion

    #region Commands

    public ICommand ShowDialogCommand { get; }
    public ICommand ShowLogCommand { get; }
    public ICommand ShowWelcomeWindowCommand { get; }
    public ICommand RefreshDataOutputCommand { get; }
    public ICommand GCCollectCommand { get; }
    public ICommand ThrowUnhandledExceptionCommand { get; }
    public ICommand ThrowUnhandledExceptionAsyncCommand { get; }
    public ICommand RunInstallerCommand { get; }
    public ICommand ShutdownAppCommand { get; }
    public ICommand UpdateThemeCommand { get; }
    public ICommand RunLoadOperationCommand { get; }

    #endregion

    #region Private Fields

    private bool _showGridLines;

    #endregion

    #region Services

    private AppUserData Data { get; }
    private AppUIManager UI { get; }
    private FileManager FileManager { get; }
    private IBrowseUIManager BrowseUI { get; }
    private IMessageUIManager MessageUI { get; }
    private LoggerManager LoggerManager { get; }
    private AppDataManager AppDataManager { get; }
    private GamesManager GamesManager { get; }

    #endregion

    #region Public Properties

    public override AppPage Page => AppPage.Debug;

    /// <summary>
    /// The selected dialog type
    /// </summary>
    public DebugDialogType SelectedDialog { get; set; }

    public bool ShowDialogAsAsync { get; set; }

    /// <summary>
    /// The selected data output type
    /// </summary>
    public DebugDataOutputType SelectedDataOutputType { get; set; }

    /// <summary>
    /// The current data output
    /// </summary>
    public string? DataOutput { get; set; }

    /// <summary>
    /// The available game installers
    /// </summary>
    public ObservableCollection<GameDescriptor>? AvailableInstallers { get; set; }

    /// <summary>
    /// The selected game installer
    /// </summary>
    public GameDescriptor? SelectedInstaller { get; set; }

    public Color SelectedAccentColor { get; set; }

    public bool ShowGridLines
    {
        get => _showGridLines;
        set
        {
            _showGridLines = value;

            Application app = Application.Current;
            
            if (_showGridLines)
                app.Resources.Add(typeof(Grid), new Style(typeof(Grid))
                {
                    Setters =
                    {
                        new Setter(Grid.ShowGridLinesProperty, true)
                    }
                });
            else
                app.Resources.Remove(typeof(Grid));
        }
    }

    public bool ForceShowAprilFoolsGifs
    {
        get => AprilFoolsGif.ForceShow;
        set => AprilFoolsGif.ForceShow = value;
    }

    #endregion

    #region Methods

    protected override Task InitializeAsync()
    {
        // Set properties
        AvailableInstallers = GamesManager.GetGameDescriptors().
            Where(x => x.GetAddActions().OfType<DiscInstallGameAddAction>().Any()).
            ToObservableCollection();
        SelectedInstaller = AvailableInstallers.First();
        SelectedAccentColor = ThemeManager.Current.DetectTheme(Metro.App.Current)?.PrimaryAccentColor ?? new Color();

        return Task.CompletedTask;
    }

    /// <summary>
    /// Shows the selected dialog
    /// </summary>
    /// <returns></returns>
    public async Task ShowDialogAsync()
    {
        if (ShowDialogAsAsync)
            await Task.Run(showAsync);
        else
            await showAsync();

        async Task showAsync()
        {
            switch (SelectedDialog)
            {
                case DebugDialogType.Message:
                    await MessageUI.DisplayMessageAsync("Debug message", "Debug header", MessageType.Information);
                    break;

                case DebugDialogType.Directory:
                    await BrowseUI.BrowseDirectoryAsync(new DirectoryBrowserViewModel()
                    {
                        Title = "Debug"
                    });
                    break;

                case DebugDialogType.Drive:
                    await UI.BrowseDriveAsync(new DriveBrowserViewModel()
                    {
                        Title = "Debug"
                    });
                    break;

                case DebugDialogType.File:
                    await BrowseUI.BrowseFileAsync(new FileBrowserViewModel()
                    {
                        Title = "Debug"
                    });
                    break;

                case DebugDialogType.SaveFile:
                    await BrowseUI.SaveFileAsync(new SaveFileViewModel()
                    {
                        Title = "Debug"
                    });
                    break;

                case DebugDialogType.EditJumpList:
                    await UI.EditJumpListAsync(new JumpListEditViewModel()
                    {
                        Title = "Debug"
                    });
                    break;

                case DebugDialogType.ItemSelection:
                    await UI.SelectItemAsync(new ItemSelectionDialogViewModel(new string[]
                    {
                        "Item 1",
                        "Item 2",
                    }, "Select an item")
                    {
                        Title = "Debug"
                    });
                    break;

                case DebugDialogType.StringInput:
                    await UI.GetStringInputAsync(new StringInputViewModel()
                    {
                        Title = "Debug",
                        HeaderText = "Specify a string"
                    });
                    break;

                case DebugDialogType.ProgramSelection:
                    await UI.GetProgramAsync(new ProgramSelectionViewModel()
                    {
                        Title = "Debug",
                    });
                    break;

                case DebugDialogType.GamesSelection_Single:
                    await UI.SelectGamesAsync(new GamesSelectionViewModel(GamesManager.GetInstalledGames())
                    {
                        Title = "Debug",
                        MultiSelection = false,
                    });
                    break;

                case DebugDialogType.GamesSelection_Multiple:
                    await UI.SelectGamesAsync(new GamesSelectionViewModel(GamesManager.GetInstalledGames())
                    {
                        Title = "Debug",
                        MultiSelection = true,
                    });
                    break;

                case DebugDialogType.AnniversaryUpdate:
                    await UI.ShowAnniversaryUpdateAsync();
                    break;

                default:
                    await MessageUI.DisplayMessageAsync("Invalid selection");
                    break;
            }
        }
    }

    /// <summary>
    /// Shows the log viewer
    /// </summary>
    /// <returns>The task</returns>
    public async Task ShowLogAsync()
    {
        if (!LoggerManager.IsLogViewerAvailable)
        {
            await MessageUI.DisplayMessageAsync("The log viewer is not enabled. Use the launch argument -logviewer to enabled it. The log file will open instead.", MessageType.Warning);

            await FileManager.LaunchFileAsync(AppFilePaths.LogFile);

            return;
        }

        LogViewer.Open(LoggerManager.LogViewerViewModel);
    }

    /// <summary>
    /// Shows the welcome window
    /// </summary>
    public void ShowWelcomeWindow()
    {
        new FirstLaunchInfoDialog().ShowDialog();
    }

    /// <summary>
    /// Refreshes the data output
    /// </summary>
    /// <returns>The task</returns>
    public void RefreshDataOutput()
    {
        try
        {
            // Clear current data
            DataOutput = String.Empty;

            switch (SelectedDataOutputType)
            {
                case DebugDataOutputType.ReferencedAssemblies:
                    foreach (AssemblyName assemblyName in Assembly.GetEntryAssembly()?.GetReferencedAssemblies() ?? Array.Empty<AssemblyName>())
                    {
                        Assembly? assembly = null;

                        try
                        {
                            // Load the assembly
                            assembly = Assembly.Load(assemblyName);
                        }
                        catch (Exception ex)
                        {
                            Logger.Warn(ex, "Loading referenced assembly");
                        }

                        DataOutput += $"Name: {assemblyName.Name}{Environment.NewLine}";
                        DataOutput += $"FullName: {assemblyName.FullName}{Environment.NewLine}";
                        DataOutput += $"Version: {assemblyName.Version}{Environment.NewLine}";

                        if (assembly != null)
                        {
                            // Get the PEKind for the assembly
                            assembly.GetModules()[0].GetPEKind(out PortableExecutableKinds peKinds, out ImageFileMachine _);
                            PortableExecutableKinds referenceKind = peKinds;

                            DataOutput += $"Location: {assembly.Location}{Environment.NewLine}";
                            DataOutput += $"ProcessorArchitecture: {((referenceKind & PortableExecutableKinds.Required32Bit) > 0 ? "x86" : (referenceKind & PortableExecutableKinds.PE32Plus) > 0 ? "x64" : "Any CPU")}{Environment.NewLine}";
                            DataOutput += $"TargetFrameworkDisplayName: {assembly.GetCustomAttributes(true).OfType<TargetFrameworkAttribute>().FirstOrDefault()?.FrameworkDisplayName}{Environment.NewLine}";
                        }

                        DataOutput += Environment.NewLine;
                    }
                    break;

                case DebugDataOutputType.AppUserData:
                    // Save app user data to update the file
                    AppDataManager.Save();

                    // Display the file contents
                    DataOutput = File.ReadAllText(AppFilePaths.AppUserDataPath);

                    break;

                case DebugDataOutputType.AppWindows:
                    var mainWindow = Application.Current.MainWindow;

                    foreach (Window window in Application.Current.Windows)
                    {
                        DataOutput += Environment.NewLine;
                        DataOutput += $"Type: {window.GetType().FullName}" + Environment.NewLine;
                        DataOutput += $"Title: {window.Title}" + Environment.NewLine;
                        DataOutput += $"Owner: {window.Owner}" + Environment.NewLine;
                        DataOutput += $"Owned windows: {window.OwnedWindows.Count}" + Environment.NewLine;
                        DataOutput += $"Show in taskbar: {window.ShowInTaskbar}" + Environment.NewLine;
                        DataOutput += $"Show activated: {window.ShowActivated}" + Environment.NewLine;
                        DataOutput += $"Visibility: {window.Visibility}" + Environment.NewLine;
                        DataOutput += $"Is main window: {window == mainWindow}" + Environment.NewLine;
                        DataOutput += Environment.NewLine;
                        DataOutput += "------------------------------------------";
                        DataOutput += Environment.NewLine;
                    }
                        
                    break;

                case DebugDataOutputType.Finder:
                    List<FinderItem> finderItems = new();

                    foreach (GameDescriptor gameDescriptor in GamesManager.GetGameDescriptors())
                    {
                        GameFinderItem? finderItem = gameDescriptor.GetFinderItem();
                        if (finderItem == null)
                            continue;
                        finderItems.Add(finderItem);
                    }

                    // Create a finder
                    Finder finder = new(Finder.DefaultOperations, finderItems.ToArray());

                    Stopwatch timer = Stopwatch.StartNew();

                    // Run the finder
                    finder.Run();

                    timer.Stop();

                    // Output the found items
                    DataOutput = finder.FinderItems.
                        Where(x => x.HasBeenFound).
                        Select(x => $"{x.ItemId} - {x.FoundLocation} ({x.FoundQuery!.GetType().Name})").JoinItems(Environment.NewLine);

                    DataOutput += Environment.NewLine;
                    DataOutput += Environment.NewLine;
                    DataOutput += $"{timer.ElapsedMilliseconds} ms";

                    break;

                case DebugDataOutputType.GameSizes:

                    // IDEA: Update with new system when done

                    var totalTime = 0L;

                    // Enumerate each game
                    foreach (GameInstallation installedGame in GamesManager.GetInstalledGames())
                    {
                        try
                        {
                            Stopwatch gameTimer = Stopwatch.StartNew();

                            // Get the size
                            long size = (installedGame.InstallLocation.HasFile 
                                ? installedGame.InstallLocation.FilePath 
                                : installedGame.InstallLocation.Directory).GetSize();

                            gameTimer.Stop();

                            totalTime += gameTimer.ElapsedMilliseconds;

                            // Output the size
                            DataOutput += $"{installedGame.GetDisplayName()}: {BinaryHelpers.BytesToString(size)} ({gameTimer.ElapsedMilliseconds} ms){Environment.NewLine}";
                        }
                        catch (Exception ex)
                        {
                            Logger.Error(ex, "Getting game install dir size");
                            DataOutput += $"{installedGame.GetDisplayName()}: N/A{Environment.NewLine}";
                        }
                    }

                    DataOutput += $"{Environment.NewLine}{totalTime} ms";

                    break;

                case DebugDataOutputType.GameIdsMarkdownTable:

                    StringBuilder sb = new();

                    // Header
                    sb.AppendLine("| Id | Game | Platform |");
                    sb.AppendLine("|---|---|---|");

                    // Add game rows
                    foreach (GameDescriptor gameDescriptor in GamesManager.GetGameDescriptors())
                    {
                        sb.AppendLine($"| {gameDescriptor.GameId} | {gameDescriptor.DisplayName} | {gameDescriptor.Platform.GetInfo().DisplayName}");
                    }

                    DataOutput = sb.ToString();

                    break;

                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Updating debug data output");
        }
    }

    public void GCCollect() => GC.Collect();

    public void ThrowUnhandledException()
    {
        throw new Exception("Debug");
    }

    public async Task ThrowUnhandledAsyncException()
    {
        await Task.Run(() => throw new Exception("Debug"));
    }

    /// <summary>
    /// Runs the selected installer
    /// </summary>
    public async Task RunInstallerAsync()
    {
        if (SelectedInstaller != null)
            await UI.InstallGameAsync(SelectedInstaller, SelectedInstaller.GetAddActions().OfType<DiscInstallGameAddAction>().First().InstallerInfo);
    }

    public void UpdateTheme()
    {
        Metro.App.Current.SetTheme(Data.Theme_DarkMode, false, SelectedAccentColor);
    }
    
    public async Task RunLoadOperationAsync()
    {
        using (LoaderLoadState state = await App.LoaderViewModel.RunAsync("Debug load operation"))
        {
            await Task.Run(async () =>
            {
                try
                {
                    const int max = 800;
                    for (int i = 0; i < max; i++)
                    {
                        state.SetProgress(new Progress(i, max));
                        await Task.Delay(10, state.CancellationToken);

                        if (i == max / 2)
                            state.SetCanCancel(true);
                    }

                    state.Complete();
                }
                catch (OperationCanceledException)
                {
                    // Ignore
                }
            });
        }
    }

    #endregion

    #region Enums

    /// <summary>
    /// The available debug dialog types
    /// </summary>
    public enum DebugDialogType
    {
        /// <summary>
        /// A message dialog
        /// </summary>
        Message,

        /// <summary>
        /// A directory selection dialog
        /// </summary>
        Directory,

        /// <summary>
        /// A drive selection dialog
        /// </summary>
        Drive,

        /// <summary>
        /// A file selection dialog
        /// </summary>
        File,

        /// <summary>
        /// A save file dialog
        /// </summary>
        SaveFile,

        EditJumpList,

        ItemSelection,

        StringInput,

        ProgramSelection,

        GamesSelection_Single,
        GamesSelection_Multiple,

        AnniversaryUpdate,
    }

    /// <summary>
    /// The available debug data output types
    /// </summary>
    public enum DebugDataOutputType
    {
        /// <summary>
        /// Displays a list of the referenced assemblies
        /// </summary>
        ReferencedAssemblies,

        /// <summary>
        /// Displays the app user data file contents
        /// </summary>
        AppUserData,

        /// <summary>
        /// Displays a list of windows in the current application
        /// </summary>
        AppWindows,

        /// <summary>
        /// Runs the finder, searching for all games and game clients and displaying the results
        /// </summary>
        Finder,

        /// <summary>
        /// Displays the install sizes for each game
        /// </summary>
        GameSizes,

        /// <summary>
        /// Displays a markdown table for the game ids
        /// </summary>
        GameIdsMarkdownTable,
    }

    #endregion
}