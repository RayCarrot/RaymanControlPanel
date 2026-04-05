using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Windows.Input;
using Nito.AsyncEx;
using RayCarrot.RCP.Metro.Games.Components;
using RayCarrot.RCP.Metro.Games.Data;
using RayCarrot.RCP.Metro.Games.Options;
using RayCarrot.RCP.Metro.Games.Panels;
using RayCarrot.RCP.Metro.Games.SetupGame;
using RayCarrot.RCP.Metro.Games.Structure;
using RayCarrot.RCP.Metro.ModLoader.Library;

namespace RayCarrot.RCP.Metro.Pages.Games;

public class InstalledGameViewModel : BaseViewModel
{
    #region Constructor

    public InstalledGameViewModel(GameInstallation gameInstallation, GameCategoryViewModel gameCategory, GameGroupViewModel gameGroup)
    {
        // Set properties
        GameInstallation = gameInstallation;
        GameCategory = gameCategory;
        GameGroup = gameGroup;
        DisplayName = gameInstallation.GetDisplayName();
        RefreshGameIcon();
        RefreshGameBanner();

        // Get and set platform info
        GamePlatformInfoAttribute platformInfo = gameInstallation.GameDescriptor.Platform.GetInfo();
        PlatformDisplayName = platformInfo.DisplayName;
        PlatformIcon = platformInfo.Icon;

        // Create collections
        GamePanels = new ObservableCollection<GamePanelViewModel>();
        GamePanels.EnableCollectionSynchronization();
        AdditionalLaunchActions = new ObservableActionItemsCollection();
        AdditionalLaunchActions.EnableCollectionSynchronization();

        // Set other properties
        SetupGameViewModel = new SetupGameViewModel(GameInstallation);
        CanUninstall = gameInstallation.GetObject<RCPGameInstallData>(GameDataKey.RCP_GameInstallData) != null;
        IsFavorite = GameInstallation.GetValue<bool>(GameDataKey.RCP_IsFavorite);

        // Create commands
        LaunchCommand = new AsyncRelayCommand(LaunchAsync);
        CloseRunningGameCommand = new AsyncRelayCommand(CloseRunningGameAsync);
        OpenGameClientGameSettingsCommand = new AsyncRelayCommand(OpenGameClientGameSettingsAsync);
        OpenGameSettingsCommand = new AsyncRelayCommand(OpenGameSettingsAsync);
        RenameCommand = new AsyncRelayCommand(RenameAsync);
        RemoveCommand = new AsyncRelayCommand(RemoveAsync);
        UninstallCommand = new AsyncRelayCommand(UninstallAsync);
        CreateShortcutCommand = new AsyncRelayCommand(CreateShortcutAsync);
        ToggleFavoriteCommand = new RelayCommand(ToggleFavorite);
        ReplaceIconImageCommand = new AsyncRelayCommand(ReplaceIconImageAsync);
        RemoveCustomIconImageCommand = new RelayCommand(RemoveCustomIconImage);
        ReplaceBannerImageCommand = new AsyncRelayCommand(ReplaceBannerImageAsync);
        RemoveCustomBannerImageCommand = new RelayCommand(RemoveCustomBannerImage);
        OpenGameDebugCommand = new AsyncRelayCommand(OpenGameDebugAsync);
    }

    #endregion

    #region Logger

    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    #endregion

    #region Private Fields

    private readonly AsyncLock LoadLock = new();
    
    private bool _loaded;
    

    #endregion

    #region Commands

    public ICommand LaunchCommand { get; }
    public ICommand CloseRunningGameCommand { get; }
    public ICommand OpenGameClientGameSettingsCommand { get; }
    public ICommand OpenGameSettingsCommand { get; }
    public ICommand RenameCommand { get; }
    public ICommand RemoveCommand { get; }
    public ICommand UninstallCommand { get; }
    public ICommand CreateShortcutCommand { get; }
    public ICommand ToggleFavoriteCommand { get; }
    public ICommand ReplaceIconImageCommand { get; }
    public ICommand RemoveCustomIconImageCommand { get; }
    public ICommand ReplaceBannerImageCommand { get; }
    public ICommand RemoveCustomBannerImageCommand { get; }
    public ICommand OpenGameDebugCommand { get; }

    #endregion

    #region Public Properties

    // Keep this here for the UI to bind to
    public AppUserData Data => Services.Data;

    public GameInstallation GameInstallation { get; }
    public GameCategoryViewModel GameCategory { get; }
    public GameGroupViewModel GameGroup { get; }
    public GameDescriptor GameDescriptor => GameInstallation.GameDescriptor;
    public LocalizedString DisplayName { get; set; }

    public LocalizedString PlatformDisplayName { get; }
    public GamePlatformIconAsset PlatformIcon { get; }

    public GameType Type => GameDescriptor.Type;
    public string GameIcon { get; set; }
    public bool HasCustomGameIcon { get; set; }
    public string GameBanner { get; set; }
    public bool HasCustomGameBanner { get; set; }

    public ObservableCollection<GamePanelViewModel> GamePanels { get; }
    public ObservableActionItemsCollection AdditionalLaunchActions { get; }

    public bool IsRunning { get; set; }

    /// <summary>
    /// Indicates if the game can be uninstalled through the Rayman Control Panel
    /// </summary>
    public bool CanUninstall { get; }

    public bool HasGameClientGameSettings { get; set; }
    public bool HasGameSettings { get; set; }

    /// <summary>
    /// The game info items
    /// </summary>
    public ObservableCollection<DuoGridItemViewModel>? GameInfoItems { get; set; }

    public ObservableCollection<GameOptionsViewModel>? GameOptions { get; set; }

    public SetupGameViewModel SetupGameViewModel { get; }

    public bool IsFavorite { get; private set; }

    public bool CanCreateShortcut { get; set; }

    #endregion

    #region Private Methods

    private async Task AddGamePanelsAsync()
    {
        foreach (GamePanelViewModel gamePanel in GamePanels)
            await gamePanel.UnloadAsync();

        GamePanels.Clear();

        foreach (GamePanelViewModel panel in GameInstallation.GetComponents<GamePanelComponent>().CreateManyObjects())
            GamePanels.Add(panel);
    }

    private void AddAdditionalLaunchActions()
    {
        AdditionalLaunchActions.Clear();

        // Add additional launch actions
        foreach (IEnumerable<ActionItemViewModel> launchActions in GameInstallation.
                     GetComponents<AdditionalLaunchActionsComponent>().
                     CreateObjects())
        {
            AdditionalLaunchActions.AddGroup(launchActions);
        }

        // Add local uri links
        AdditionalLaunchActions.AddGroup(GameInstallation.GetComponents<LocalGameLinksComponent>().
            CreateManyObjects().
            Where(x => File.Exists(x.Uri)).
            Select<GameLinksComponent.GameUriLink, ActionItemViewModel>(x =>
            {
                // Get the path
                string path = x.Uri;

                // Create the command
                AsyncRelayCommand command = new(async () =>
                    (await Services.File.LaunchFileAsync(path, arguments: x.Arguments))?.Dispose());

                if (x.Icon != GenericIconKind.None)
                    return new IconCommandItemViewModel(
                        header: x.Header,
                        description: path,
                        iconKind: x.Icon,
                        command: command);

                try
                {
                    return new ImageCommandItemViewModel(
                        header: x.Header,
                        description: path,
                        imageSource: WindowsHelpers.GetIconOrThumbnail(path, ShellThumbnailSize.Small).ToImageSource(),
                        command: command);
                }
                catch (Exception ex)
                {
                    Logger.Error(ex, "Getting file icon for drop-down action item");

                    return new IconCommandItemViewModel(
                        header: x.Header,
                        description: path,
                        iconKind: GenericIconKind.None,
                        command: command);
                }

            }));

        // Add external uri links
        AdditionalLaunchActions.AddGroup(GameInstallation.GetComponents<ExternalGameLinksComponent>().
            CreateManyObjects().
            Select<GameLinksComponent.GameUriLink, ActionItemViewModel>(x =>
            {
                // Get the path
                string path = x.Uri;

                // Create the command
                AsyncRelayCommand command = new(async () =>
                    (await Services.File.LaunchFileAsync(path, arguments: x.Arguments))?.Dispose());

                if (x.AssetIcon != null)
                    return new ImageCommandItemViewModel(
                        header: x.Header,
                        description: path,
                        assetValue: x.AssetIcon,
                        command: command);

                return new IconCommandItemViewModel(
                    header: x.Header,
                    description: path,
                    iconKind: x.Icon,
                    command: command);
            }));

        // Add RayMap link (for now we assume there's only a single RayMap component per game)
        RayMapComponent? rayMapComponent = GameInstallation.GetComponent<RayMapComponent>();
        if (rayMapComponent != null)
        {
            string url = rayMapComponent.GetURL();
            AdditionalLaunchActions.AddGroup(new ImageCommandItemViewModel(
                header: Resources.GameDisplay_Raymap, 
                description: url,
                assetValue: rayMapComponent.GetIcon(), 
                command: new AsyncRelayCommand(async () => (await Services.File.LaunchFileAsync(url))?.Dispose())));
        }

        // Add open location (don't add as a group since it's the last item)
        AdditionalLaunchActions.Add(new IconCommandItemViewModel(
            header: Resources.GameDisplay_OpenLocation, 
            description: GameInstallation.InstallLocation.Directory.FullPath,
            iconKind: GenericIconKind.GameAction_Location, 
            command: new AsyncRelayCommand(async () =>
            {
                // Get the install location to open
                FileSystemPath pathToOpen;

                if (GameInstallation.InstallLocation.HasFile)
                {
                    pathToOpen = GameInstallation.InstallLocation.FilePath;
                }
                else
                {
                    pathToOpen = GameInstallation.InstallLocation.Directory;

                    // Select the exe file in Explorer if it exists
                    if (GameInstallation.GameDescriptor.Structure is DirectoryProgramInstallationStructure structure)
                    {
                        FileSystemPath exeFilePath = structure.FileSystem.GetAbsolutePath(GameInstallation, ProgramPathType.PrimaryExe);

                        if (exeFilePath.FileExists && exeFilePath.Parent == pathToOpen)
                            pathToOpen = exeFilePath;
                    }
                }

                // Open the location
                await Services.File.OpenExplorerLocationAsync(pathToOpen);

                Logger.Trace("The game {0} install location was opened", GameInstallation.FullId);
            })));
    }

    private void InitializeGameOptions()
    {
        if (GameOptions == null)
            return;

        foreach (GameOptionsViewModel gameOptionsViewModel in GameOptions)
        {
            if (gameOptionsViewModel is IInitializable initializable)
                initializable.Initialize();
        }
    }

    private void DeinitializeGameOptions()
    {
        if (GameOptions == null) 
            return;
        
        foreach (GameOptionsViewModel gameOptionsViewModel in GameOptions)
        {
            if (gameOptionsViewModel is IInitializable initializable)
                initializable.Deinitialize();
        }
    }

    private async Task ReloadAsync(bool rebuiltComponents)
    {
        using (await LoadLock.LockAsync())
        {
            if (rebuiltComponents)
            {
                // Only reload options if rebuilt components
                DeinitializeGameOptions();

                var options = GameInstallation.GetComponents<GameOptionsComponent>().CreateObjects();
                GameOptions = new ObservableCollection<GameOptionsViewModel>(options);
                InitializeGameOptions();

                CanCreateShortcut = GameInstallation.HasComponent<LaunchGameComponent>();

                // Check for game settings availability
                HasGameClientGameSettings = GameInstallation.HasComponent<GameClientGameSettingsComponent>();
                HasGameSettings = GameInstallation.HasComponent<GameSettingsComponent>();
            }

            // Load info
            GameInfoItems = new ObservableCollection<DuoGridItemViewModel>(GameInstallation.GetComponents<GameInfoComponent>().CreateManyObjects());

            // Load additional launch actions
            AddAdditionalLaunchActions();

            // Check if running
            IsRunning = Services.RunningGamesManager.IsGameRunning(GameInstallation);

            // Load setup game
            await SetupGameViewModel.LoadAsync();

            // Load panels
            await AddGamePanelsAsync();

            await Task.WhenAll(GamePanels.Select(x => x.LoadAsync()));

            Logger.Info("Loaded game {0}", GameInstallation.FullId);
        }
    }

    [MemberNotNull(nameof(GameIcon))]
    private void RefreshGameIcon()
    {
        GameIcon = GameInstallation.GetIconAssetSource();
        HasCustomGameIcon = GameInstallation.GetValue<string?>(GameDataKey.RCP_IconImage) != null;
    }

    [MemberNotNull(nameof(GameBanner))]
    private void RefreshGameBanner()
    {
        GameBanner = GameInstallation.GetBannerAssetSource();
        HasCustomGameBanner = GameInstallation.GetValue<string?>(GameDataKey.RCP_BannerImage) != null;
    }

    #endregion

    #region Public Methods

    public Task LoadAsync()
    {
        if (_loaded)
            return Task.CompletedTask;

        _loaded = true;

        return ReloadAsync(true);
    }

    public void Unload()
    {
        DeinitializeGameOptions();
    }

    public Task RefreshAsync(bool rebuiltComponents)
    {
        // Always update the display name
        DisplayName = GameInstallation.GetDisplayName();

        if (!_loaded)
            return Task.CompletedTask;

        // Only reload options if the components were rebuilt
        return ReloadAsync(rebuiltComponents: rebuiltComponents);
    }

    public async Task LaunchAsync()
    {
        LaunchGameComponent? launchComponent = GameInstallation.GetComponent<LaunchGameComponent>();

        if (launchComponent == null)
        {
            await Services.MessageUI.DisplayMessageAsync(Resources.Games_MissingLaunchComponent, MessageType.Error);
            return;
        }

        await launchComponent.LaunchAsync();
    }

    public async Task CloseRunningGameAsync()
    {
        try
        {
            Services.RunningGamesManager.CloseGame(GameInstallation);
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Closing running game");   

            await Services.MessageUI.DisplayExceptionMessageAsync(ex, Resources.CloseGameError);
        }
    }

    public Task OpenGameClientGameSettingsAsync()
    {
        return Services.UI.ShowGameClientGameOptionsAsync(GameInstallation);
    }

    public Task OpenGameSettingsAsync()
    {
        return Services.UI.ShowGameSettingsAsync(GameInstallation);
    }

    public async Task RenameAsync()
    {
        StringInputResult result = await Services.UI.GetStringInputAsync(new StringInputViewModel
        {
            Title = Resources.Games_RenameHeader,
            HeaderText = String.Format(Resources.Games_RenameInfo, GameDescriptor.DisplayName),
            StringInput = GameInstallation.GetValue<string>(GameDataKey.RCP_CustomName),
        });

        if (result.CanceledByUser)
            return;

        string? name = result.StringInput;

        if (name.IsNullOrWhiteSpace())
            name = null;

        GameInstallation.SetValue(GameDataKey.RCP_CustomName, name);

        Logger.Info("Renamed the game {0} to {1}", GameInstallation.FullId, name);

        Services.Messenger.Send(new ModifiedGamesMessage(GameInstallation));
    }

    /// <summary>
    /// Removes the game from the program
    /// </summary>
    /// <returns>The task</returns>
    public async Task RemoveAsync()
    {
        // Ask the user
        if (!await Services.MessageUI.DisplayMessageAsync(String.Format(CanUninstall ? Resources.RemoveInstalledGameQuestion : Resources.RemoveGameQuestion, DisplayName), Resources.RemoveGameQuestionHeader, MessageType.Question, true))
            return;

        // Check if there are applied mods
        if (GameInstallation.GetComponents<ModModuleComponent>().Any())
        {
            // Get applied mods
            ModManifest? modManifest = null;

            try
            {
                ModLibrary library = new(GameInstallation);
                modManifest = library.ReadModManifest();
            }
            catch (Exception ex)
            {
                Logger.Warn(ex, "Reading mod manifest");
            }

            int appliedModsCount = modManifest?.Mods.Values.Count(x => x.IsEnabled) ?? 0;

            // Warn about applied mods, if any
            if (appliedModsCount > 0)
            {
                if (!await Services.MessageUI.DisplayMessageAsync(String.Format(Resources.Games_VerifyRemoveWithMods, appliedModsCount), 
                        MessageType.Warning, true)) 
                    return;
            }
        }

        // Remove the game
        await Services.Games.RemoveGameAsync(GameInstallation);
    }

    /// <summary>
    /// Uninstalls the game
    /// </summary>
    /// <returns>The task</returns>
    public async Task UninstallAsync()
    {
        RCPGameInstallData? installData = GameInstallation.GetObject<RCPGameInstallData>(GameDataKey.RCP_GameInstallData);

        if (installData == null)
        {
            Logger.Warn("Can't uninstall a game without install data");
            return;
        }

        Logger.Info("{0} is being uninstalled...", GameInstallation.FullId);

        // Have user confirm
        if (!await Services.MessageUI.DisplayMessageAsync(String.Format(Resources.UninstallGameQuestion, DisplayName), Resources.UninstallGameQuestionHeader, MessageType.Question, true))
        {
            Logger.Info("The uninstallation was canceled");

            return;
        }

        try
        {
            // Delete the game directory
            Services.File.DeleteDirectory(installData.InstallDir);

            Logger.Info("The game install directory was removed");
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Uninstalling game {0}", GameInstallation.FullId);
            await Services.MessageUI.DisplayExceptionMessageAsync(ex, String.Format(Resources.UninstallGameError, DisplayName), Resources.UninstallGameErrorHeader);

            return;
        }

        // Remove the game
        await Services.Games.RemoveGameAsync(GameInstallation);

        await Services.MessageUI.DisplaySuccessfulActionMessageAsync(String.Format(Resources.UninstallGameSuccess, DisplayName), Resources.UninstallGameSuccessHeader);
    }

    /// <summary>
    /// Creates a shortcut to launch the game
    /// </summary>
    /// <returns>The task</returns>
    public async Task CreateShortcutAsync()
    {
        try
        {
            LaunchGameComponent component = GameInstallation.GetRequiredComponent<LaunchGameComponent>();

            var result = await Services.BrowseUI.BrowseDirectoryAsync(new DirectoryBrowserViewModel()
            {
                DefaultDirectory = Environment.SpecialFolder.Desktop.GetFolderPath(),
                Title = Resources.GameShortcut_BrowseHeader
            });

            if (result.CanceledByUser)
                return;

            string shortcutName = String.Format(Resources.GameShortcut_ShortcutName, GameInstallation.GetDisplayName());

            component.CreateShortcut(shortcutName, result.SelectedDirectory);

            await Services.MessageUI.DisplaySuccessfulActionMessageAsync(Resources.GameShortcut_Success);
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Creating game shortcut {0}", GameInstallation.FullId);
            await Services.MessageUI.DisplayExceptionMessageAsync(ex, Resources.GameShortcut_Error, Resources.GameShortcut_ErrorHeader);
        }
    }

    public void ToggleFavorite()
    {
        IsFavorite = !GameInstallation.GetValue<bool>(GameDataKey.RCP_IsFavorite);
        GameInstallation.SetValue(GameDataKey.RCP_IsFavorite, IsFavorite);
    }

    public async Task ReplaceIconImageAsync()
    {
        // TODO-LOC
        FileBrowserResult result = await Services.BrowseUI.BrowseFileAsync(new FileBrowserViewModel()
        {
            Title = "Select icon image",
            ExtensionFilter = "Image files|*.png;*.jpg;*.jpeg;*.bmp"
        });

        if (result.CanceledByUser)
            return;

        GameInstallation.SetValue<string>(GameDataKey.RCP_IconImage, result.SelectedFile);
        Services.Messenger.Send(new ModifiedGameIconMessage(GameInstallation));
        RefreshGameIcon();
    }

    public void RemoveCustomIconImage()
    {
        GameInstallation.SetValue<string?>(GameDataKey.RCP_IconImage, null);
        Services.Messenger.Send(new ModifiedGameIconMessage(GameInstallation));
        RefreshGameIcon();
    }

    public async Task ReplaceBannerImageAsync()
    {
        // TODO-LOC
        FileBrowserResult result = await Services.BrowseUI.BrowseFileAsync(new FileBrowserViewModel()
        {
            Title = "Select banner image",
            ExtensionFilter = "Image files|*.png;*.jpg;*.jpeg;*.bmp"
        });

        if (result.CanceledByUser)
            return;

        GameInstallation.SetValue<string>(GameDataKey.RCP_BannerImage, result.SelectedFile);
        RefreshGameBanner();
    }

    public void RemoveCustomBannerImage()
    {
        GameInstallation.SetValue<string?>(GameDataKey.RCP_BannerImage, null);
        RefreshGameBanner();
    }

    public Task OpenGameDebugAsync() => Services.UI.ShowGameDebugAsync(GameInstallation);

    #endregion
}