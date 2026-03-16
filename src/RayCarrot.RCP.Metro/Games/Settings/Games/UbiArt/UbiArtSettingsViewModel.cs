using Microsoft.Win32;

namespace RayCarrot.RCP.Metro.Games.Settings;

/// <summary>
/// View model for the Rayman Origins and Rayman Legends settings
/// </summary>
public class UbiArtSettingsViewModel : GameSettingsViewModel
{
    #region Constructor

    public UbiArtSettingsViewModel(GameInstallation gameInstallation, string registryKey) : base(gameInstallation)
    {
        GraphicsMode = new GraphicsModeSelectionViewModel();
        GraphicsMode.GraphicsModeChanged += (_, _) => UnsavedChanges = true;

        RegistryKey = registryKey;

        CommandArgsViewModel = new UbiArtCommandArgsViewModel(gameInstallation);
        CommandArgsViewModel.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName is 
                nameof(UbiArtCommandArgsViewModel.Source) or 
                nameof(UbiArtCommandArgsViewModel.Text))
                UnsavedChanges = true;
        };
    }

    #endregion

    #region Logger

    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    #endregion

    #region Private Constants

    private const string ScreenHeightKey = "ScreenHeight";
    private const string ScreenWidthKey = "ScreenWidth";
    private const string FullScreenKey = "FullScreen";

    #endregion

    #region Public Properties

    public GraphicsModeSelectionViewModel GraphicsMode { get; }

    public string RegistryKey { get; }
    public bool FullscreenMode { get; set; }

    public UbiArtCommandArgsViewModel CommandArgsViewModel { get; }

    #endregion

    #region Private Methods

    /// <summary>
    /// Gets the registry key for the game properties
    /// </summary>
    /// <param name="writable">True if the key should be writable</param>
    /// <returns>The key</returns>
    private RegistryKey? GetKey(bool writable)
    {
        // Get the key path
        string keyPath = RegistryHelpers.CombinePaths(RegistryKey, "Settings");

        // Create the key if it doesn't exist and should be written to
        if (!RegistryHelpers.KeyExists(keyPath) && writable)
        {
            RegistryKey? key = RegistryHelpers.CreateRegistryKey(keyPath, RegistryView.Default, true);

            Logger.Info("The Registry key {0} has been created", key?.Name);

            return key;
        }

        return RegistryHelpers.GetKeyFromFullPath(keyPath, RegistryView.Default, writable);
    }

    #endregion

    #region Protected Methods

    protected override async Task LoadAsync()
    {
        AddSettingsLocation(LinkItemViewModel.LinkType.RegistryKey, RegistryKey);

        using (RegistryKey? key = GetKey(false))
        {
            if (key != null)
                Logger.Info("The key {0} has been opened", key.Name);
            else
                Logger.Info("The key for {0} does not exist. Default values will be used.", GameInstallation.FullId);

            GraphicsMode.GetAvailableResolutions();
            GraphicsMode.SelectedGraphicsMode = new GraphicsMode(
                width: GetInt(ScreenWidthKey, WindowsHelpers.GetScreenWidth()), 
                height: GetInt(ScreenHeightKey, WindowsHelpers.GetScreenHeight()));
            FullscreenMode = GetInt(FullScreenKey, 1) == 1;

            // Helper methods for getting values
            int GetInt(string valueName, int defaultValue) => 
                Int32.TryParse(key?.GetValue(valueName, defaultValue).ToString().KeepFirstDigitsOnly(), out int result) ? result : defaultValue;
        }

        await CommandArgsViewModel.LoadAsync();

        UnsavedChanges = false;
    }

    protected override Task SaveAsync()
    {
        using (RegistryKey? key = GetKey(true))
        {
            if (key == null)
                throw new Exception("The Registry key could not be created");

            Logger.Info("The key {0} has been opened", key.Name);

            key.SetValue(ScreenWidthKey, GraphicsMode.Width.ToString());
            key.SetValue(ScreenHeightKey, GraphicsMode.Height.ToString());
            key.SetValue(FullScreenKey, FullscreenMode ? 1 : 0);
        }

        CommandArgsViewModel.Save();

        return Task.CompletedTask;
    }

    protected override void SettingsPropertyChanged(string propertyName)
    {
        if (propertyName is nameof(FullscreenMode))
        {
            UnsavedChanges = true;
        }
    }

    #endregion
}