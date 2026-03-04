#nullable disable
using System.Reflection;
using RayCarrot.RCP.Metro.Archive;
using RayCarrot.RCP.Metro.Archive.CPA;
using RayCarrot.RCP.Metro.Games.Clients;
using RayCarrot.RCP.Metro.Pages.Games;

namespace RayCarrot.RCP.Metro;

/// <summary>
/// The application user data
/// </summary>
public partial class AppUserData : BaseViewModel
{
    #region Public Methods

    /// <summary>
    /// Resets all values to their defaults
    /// </summary>
    public void Reset()
    {
        // App
        App_UserLevel = UserLevel.Advanced;
        App_LastVersion = new Version(0, 0, 0, 0);
        App_ShowActionComplete = true;
        App_CurrentCulture = LocalizationManager.DefaultCulture.Name;
        App_ShowIncompleteTranslations = false;
        App_ApplicationPath = Assembly.GetEntryAssembly()?.Location;
        App_IsFirstLaunch = true;
        App_CloseAppOnGameLaunch = false;
        App_JumpListItems = new List<JumpListItem>();
        App_AutoSortJumpList = true;
        App_DisableGameValidation = false;
        App_CachedNews = new List<AppNewsEntry>();
        App_LoadNews = true;
        App_LastYearShownAprilFoolsMessage = 0;

        // UI
        UI_WindowState = null;
        UI_ShowProgressOnTaskBar = true;
        UI_FlashWindowOnTaskBar = true;
        UI_EnableAnimations = true;
        UI_UseChildWindows = true;
        UI_ShowGameInfo = false;
        UI_GroupInstalledGames = true;
        UI_GroupProgressionGames = true;
        UI_ShowRecentGames = true;

        // Theme
        Theme_DarkMode = true;
        Theme_SyncTheme = false;

        // Game
        Game_GameInstallations = new List<GameInstallation>();
        Game_GameClientInstallations = new List<GameClientInstallation>();
        Game_AutoLocateGames = true;

        // Update
        Update_AutoUpdate = true;
        Update_ForceUpdate = false;
        Update_GetBetaUpdates = false;
        Update_DisableDowngradeWarning = false;
        Update_IsUpdateAvailable = false;

        // Backup
        Backup_BackupLocation = Environment.SpecialFolder.MyDocuments.GetFolderPath();
        Backup_CompressBackups = true;

        // Progression
        Progression_ShownEditSaveWarning = false;

        // Archive
        Archive_GF_GenerateMipmaps = true;
        Archive_GF_UpdateTransparency = GFTransparencyMode.PreserveFormat;
        Archive_GF_ForceGF8888Import = false;
        Archive_ExplorerSortOption = ArchiveItemsSort.Default;
        Archive_CNT_SyncOnRepack = false;
        Archive_CNT_SyncOnRepackRequested = false;

        // File editors
        FileEditors_AssociatedEditors = new Dictionary<string, FileSystemPath>();

        // Binary
        Binary_IsSerializationLogEnabled = false;
        Binary_BinarySerializationFileLogPath = FileSystemPath.EmptyPath;

        // Mod Loader
        ModLoader_AutomaticallyCheckForUpdates = true;
        ModLoader_ShowDownloadableModsCount = true;
        ModLoader_ViewedMods = new Dictionary<string, List<ViewedMod>>();
        ModLoader_ShowModConflictsWarning = true;
        ModLoader_ShowEssentialMods = true;
        ModLoader_MarkUnseenMods = true;
        ModLoader_IncludeDownloadableNsfwMods = false;
    }

    /// <summary>
    /// Verifies all values are valid and corrects them if not
    /// </summary>
    public void Verify()
    {
        // App
        App_LastVersion ??= new Version(0, 0, 0, 0);
        App_CurrentCulture ??= LocalizationManager.DefaultCulture.Name;
        App_JumpListItems ??= new List<JumpListItem>();
        App_CachedNews ??= new List<AppNewsEntry>();

        // Game
        Game_GameInstallations ??= new List<GameInstallation>();
        Game_GameClientInstallations ??= new List<GameClientInstallation>();

        // File editors
        FileEditors_AssociatedEditors ??= new Dictionary<string, FileSystemPath>();

        // Mod loader
        ModLoader_ViewedMods ??= new Dictionary<string, List<ViewedMod>>();
    }

    #endregion

    #region App

    /// <summary>
    /// The current user level
    /// </summary>
    public UserLevel App_UserLevel
    {
        get => Services.InstanceData.CurrentUserLevel;
        set => Services.InstanceData.CurrentUserLevel = value;
    }

    /// <summary>
    /// The last used version of the program
    /// </summary>
    public Version App_LastVersion { get; set; }

    /// <summary>
    /// Indicates if action complete messages should be shown
    /// </summary>
    public bool App_ShowActionComplete { get; set; }

    /// <summary>
    /// The current culture in the application
    /// </summary>
    public string App_CurrentCulture
    {
        get => Services.InstanceData.CurrentCulture?.Name ?? LocalizationManager.DefaultCulture.Name;
        set => LocalizationManager.SetCulture(value);
    }

    /// <summary>
    /// Indicates if languages with incomplete translations should be shown
    /// </summary>
    public bool App_ShowIncompleteTranslations { get; set; }

    /// <summary>
    /// The last recorded path of the application
    /// </summary>
    public FileSystemPath App_ApplicationPath { get; set; }

    /// <summary>
    /// Indicates if it is the first time the application launches
    /// </summary>
    public bool App_IsFirstLaunch { get; set; }

    /// <summary>
    /// Indicates if the application should close when a game is launched
    /// </summary>
    public bool App_CloseAppOnGameLaunch { get; set; }

    /// <summary>
    /// The saved jump list items
    /// </summary>
    public List<JumpListItem> App_JumpListItems { get; set; }

    /// <summary>
    /// Indicates if the jump list items should be automatically sorted
    /// </summary>
    public bool App_AutoSortJumpList { get; set; }

    /// <summary>
    /// Disables validating the game install location. This allow a game with any install location to be added.
    /// </summary>
    public bool App_DisableGameValidation { get; set; }

    /// <summary>
    /// Cached news entries for the app news
    /// </summary>
    public List<AppNewsEntry> App_CachedNews { get; set; }

    /// <summary>
    /// Indicates if the app news should be loaded
    /// </summary>
    public bool App_LoadNews { get; set; }

    /// <summary>
    /// The last year the April Fools' message was shown
    /// </summary>
    public int App_LastYearShownAprilFoolsMessage { get; set; }

    #endregion

    #region UI

    /// <summary>
    /// The previous Window session state
    /// </summary>
    public WindowSessionState UI_WindowState { get; set; }

    /// <summary>
    /// Indicates if progress should be shown on the task bar
    /// </summary>
    public bool UI_ShowProgressOnTaskBar { get; set; }

    /// <summary>
    /// Indicates if the window should flash on the taskbar for certain events
    /// </summary>
    public bool UI_FlashWindowOnTaskBar { get; set; }

    /// <summary>
    /// Indicates if animations are enabled
    /// </summary>
    public bool UI_EnableAnimations { get; set; }

    /// <summary>
    /// Indicates if dialogs should be shown as child windows whenever possible
    /// </summary>
    public bool UI_UseChildWindows { get; set; }

    /// <summary>
    /// Indicates if the game info should show in the game hub
    /// </summary>
    public bool UI_ShowGameInfo { get; set; }

    /// <summary>
    /// Indicates if the games should be grouped in the Games page
    /// </summary>
    public bool UI_GroupInstalledGames { get; set; }

    /// <summary>
    /// Indicates if the games should be grouped in the Progression page
    /// </summary>
    public bool UI_GroupProgressionGames { get; set; }

    /// <summary>
    /// Indicates if the recent games should show in the home page
    /// </summary>
    public bool UI_ShowRecentGames { get; set; }

    #endregion

    #region Theme

    /// <summary>
    /// Indicates if the dark mode is on
    /// </summary>
    public bool Theme_DarkMode { get; set; }

    /// <summary>
    /// Indicates if the theme should sync with the system theme
    /// </summary>
    public bool Theme_SyncTheme { get; set; }

    #endregion

    #region Game

    /// <summary>
    /// The installed games
    /// </summary>
    public List<GameInstallation> Game_GameInstallations { get; set; }

    /// <summary>
    /// The installed game clients
    /// </summary>
    public List<GameClientInstallation> Game_GameClientInstallations { get; set; }

    /// <summary>
    /// Indicates if games should be automatically located on startup
    /// </summary>
    public bool Game_AutoLocateGames { get; set; }

    #endregion

    #region Update

    /// <summary>
    /// Indicates if updates should automatically be check for
    /// </summary>
    public bool Update_AutoUpdate { get; set; }

    /// <summary>
    /// Indicates if a manual update should be forced even if the installed version is the latest one
    /// </summary>
    public bool Update_ForceUpdate { get; set; }

    /// <summary>
    /// Indicates if beta updates should be searched for through the updater
    /// </summary>
    public bool Update_GetBetaUpdates { get; set; }

    /// <summary>
    /// Indicates if the downgrade warnings should be disabled
    /// </summary>
    public bool Update_DisableDowngradeWarning { get; set; }

    /// <summary>
    /// Indicates if a new update is available
    /// </summary>
    public bool Update_IsUpdateAvailable { get; set; }

    #endregion

    #region Backup

    /// <summary>
    /// The backup directory path
    /// </summary>
    public FileSystemPath Backup_BackupLocation { get; set; }

    /// <summary>
    /// Indicates if backups should be compressed
    /// </summary>
    public bool Backup_CompressBackups { get; set; }

    #endregion

    #region Progression

    /// <summary>
    /// Indicates if the warning for editing save files has been shown
    /// </summary>
    public bool Progression_ShownEditSaveWarning { get; set; }

    #endregion

    #region Archive

    /// <summary>
    /// Indicates if mipmaps should be generated when importing files for .gf files (if supported)
    /// </summary>
    public bool Archive_GF_GenerateMipmaps { get; set; }

    /// <summary>
    /// Indicates if the image format should be updated depending on if the imported file supports transparency for .gf files 
    /// </summary>
    public GFTransparencyMode Archive_GF_UpdateTransparency { get; set; }

    /// <summary>
    /// Indicates if an imported .gf file should be forced to 888 (no transparency) or 8888 (with transparency)
    /// </summary>
    public bool Archive_GF_ForceGF8888Import { get; set; }

    /// <summary>
    /// The sort option for the Archive Explorer
    /// </summary>
    public ArchiveItemsSort Archive_ExplorerSortOption { get; set; }

    /// <summary>
    /// Indicates if the textures for a CNT file should be synchronized with the game on repacking the archive
    /// </summary>
    public bool Archive_CNT_SyncOnRepack { get; set; }

    /// <summary>
    /// Indicates if the user has been asked to enable <see cref="Archive_CNT_SyncOnRepack"/>
    /// </summary>
    public bool Archive_CNT_SyncOnRepackRequested { get; set; }

    #endregion

    #region File Editors

    /// <summary>
    /// The file editor file paths, associated with their file extensions. An empty string is used for a binary file editor.
    /// </summary>
    public Dictionary<string, FileSystemPath> FileEditors_AssociatedEditors { get; set; }

    #endregion

    #region Binary

    /// <summary>
    /// The binary serialization logging file path
    /// </summary>
    public FileSystemPath Binary_BinarySerializationFileLogPath { get; set; }

    /// <summary>
    /// Indicates if the serialization log is enabled
    /// </summary>
    public bool Binary_IsSerializationLogEnabled { get; set; }

    #endregion

    #region Mod Loader

    /// <summary>
    /// Indicates if the mod loader should automatically check for mod updates
    /// </summary>
    public bool ModLoader_AutomaticallyCheckForUpdates { get; set; }

    /// <summary>
    /// Indicates if the mod loader should automatically check for mod updates
    /// </summary>
    public bool ModLoader_ShowDownloadableModsCount { get; set; }

    /// <summary>
    /// Specifies which downloadable mods have been viewed, for each source
    /// </summary>
    public Dictionary<string, List<ViewedMod>> ModLoader_ViewedMods { get; set; }

    /// <summary>
    /// Indicates if a warning message should be shown if there are mod conflicts when applying
    /// </summary>
    public bool ModLoader_ShowModConflictsWarning { get; set; }

    /// <summary>
    /// Indicates if the essential (featured) mods should show first when downloading mods
    /// </summary>
    public bool ModLoader_ShowEssentialMods { get; set; }

    /// <summary>
    /// Indicates if unseen mods should be marked when downloading mods
    /// </summary>
    public bool ModLoader_MarkUnseenMods { get; set; }

    /// <summary>
    /// Indicates if downloadable mods with a content rating should be shown.
    /// </summary>
    public bool ModLoader_IncludeDownloadableNsfwMods { get; set; }

    #endregion
}