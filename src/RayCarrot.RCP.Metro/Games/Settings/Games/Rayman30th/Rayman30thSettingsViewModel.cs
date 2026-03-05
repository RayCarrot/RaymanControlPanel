using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace RayCarrot.RCP.Metro.Games.Settings;

/// <summary>
/// View model for the Rayman 30th Anniversary Edition settings
/// </summary>
public class Rayman30thSettingsViewModel : GameSettingsViewModel
{
    #region Constructor

    public Rayman30thSettingsViewModel(GameInstallation gameInstallation) : base(gameInstallation)
    {
        GraphicsMode = new GraphicsModeSelectionViewModel();
        GraphicsMode.GraphicsModeChanged += (_, _) => UnsavedChanges = true;

        FileSystemPath baseSavePath = Environment.SpecialFolder.ApplicationData.GetFolderPath() + DataDirectoryName;
        FileSystemPath savePath = UbisoftConnectHelpers.GetSaveDirectory(gameInstallation, baseSavePath);

        if (savePath != FileSystemPath.EmptyPath)
            SettingsFilePath = savePath + SettingsFileName;
        else
            SettingsFilePath = baseSavePath + SettingsFileName;

        // From options.lua
        LanguageIds = ["en-us", "fr-fr", "de-de", "it-it", "es-es", "ja-jp", "zh-tw", "zh-cn", "pt-br"];
        AvailableLanguages = new()
        {
            new ResourceLocString(nameof(Resources.Lang_English)),
            new ResourceLocString(nameof(Resources.Lang_French)),
            new ResourceLocString(nameof(Resources.Lang_German)),
            new ResourceLocString(nameof(Resources.Lang_Italian)),
            new ResourceLocString(nameof(Resources.Lang_Spanish)),
            new ResourceLocString(nameof(Resources.Lang_Japanese)),
            new ResourceLocString(nameof(Resources.Lang_TraditionalChinese)),
            new ResourceLocString(nameof(Resources.Lang_SimplifiedChinese)),
            new ResourceLocString(nameof(Resources.Lang_BrazilianPortuguese)),
        };
    }

    #endregion

    #region Private Constants

    private const string DataDirectoryName = "Rayman 30th Anniversary Edition";
    private const string SettingsFileName = "settings_main.json";

    private const string GlobalKey = "global";
    private const string ResolutionKey = "resolution";
    private const string WindowModeKey = "window_mode";
    private const string LanguageKey = "language";
    private const string MenuMusicKey = "menu_music";
    private const string VolumeKey = "volume";

    private const string WindowModeFullscreenValue = "fullscreen";
    private const string WindowModeWindowedValue = "window";

    private const string MenuMusicOnValue = "on";
    private const string MenuMusicOffValue = "off";

    #endregion

    #region Public Properties

    public FileSystemPath SettingsFilePath { get; }

    public GraphicsModeSelectionViewModel GraphicsMode { get; }
    public bool FullscreenMode { get; set; }

    public string[] LanguageIds { get; }
    public ObservableCollection<LocalizedString> AvailableLanguages { get; }
    public int SelectedLanguageIndex { get; set; }

    public bool IsMenuMusicEnabled { get; set; }
    public int Volume { get; set; }

    #endregion

    #region Private Methods

    private static bool TryParseResolutionString(string resString, out int width, out int height)
    {
        width = 0;
        height = 0;

        string[] components = resString.Split('x');
        
        if (components.Length != 2) 
            return false;
        
        if (Int32.TryParse(components[0], out width) && Int32.TryParse(components[1], out height))
            return true;

        return false;
    }

    #endregion

    #region Protected Methods

    protected override Task LoadAsync()
    {
        AddSettingsLocation(LinkItemViewModel.LinkType.File, SettingsFilePath);

        JObject? jObj = null;
        if (SettingsFilePath.FileExists)
        {
            string json = File.ReadAllText(SettingsFilePath);
            jObj = JsonConvert.DeserializeObject<JObject>(json);
        }

        JToken? global = jObj?[GlobalKey];

        // Get the resolution
        GraphicsMode.GetAvailableResolutions();
        string? resString = global?[ResolutionKey]?.Value<string>();
        if (resString != null && TryParseResolutionString(resString, out int width, out int height))
            GraphicsMode.SelectedGraphicsMode = new GraphicsMode(width, height);
        else
            GraphicsMode.SelectedGraphicsMode = new GraphicsMode(WindowsHelpers.GetScreenWidth(), WindowsHelpers.GetScreenHeight());

        // Get the window mode
        string? windowModeString = global?[WindowModeKey]?.Value<string>();
        FullscreenMode = windowModeString is null or WindowModeFullscreenValue;

        // Get the language
        string? langId = global?[LanguageKey]?.Value<string>();
        SelectedLanguageIndex = LanguageIds.IndexOf(langId);
        if (SelectedLanguageIndex == -1)
            SelectedLanguageIndex = 0;

        // Get the menu music toggle
        string? menuMusicString = global?[MenuMusicKey]?.Value<string>();
        IsMenuMusicEnabled = menuMusicString is null or MenuMusicOnValue;

        // Get the volume
        int? volumeInt = global?[VolumeKey]?.Value<int>();
        Volume = volumeInt ?? 10;

        UnsavedChanges = false;

        return Task.CompletedTask;
    }

    protected override Task SaveAsync()
    {
        JObject jObj;
        if (SettingsFilePath.FileExists)
        {
            string json = File.ReadAllText(SettingsFilePath);
            jObj = JsonConvert.DeserializeObject<JObject>(json) ?? new JObject();
        }
        else
        {
            jObj = new JObject();
        }

        if (jObj[GlobalKey] is not JObject global)
        {
            global = new JObject();
            jObj[GlobalKey] = global;
        }

        global[ResolutionKey] = $"{GraphicsMode.SelectedGraphicsMode.Width}x{GraphicsMode.SelectedGraphicsMode.Height}";
        global[WindowModeKey] = FullscreenMode ? WindowModeFullscreenValue : WindowModeWindowedValue;
        global[LanguageKey] = LanguageIds[SelectedLanguageIndex];
        global[MenuMusicKey] = IsMenuMusicEnabled ? MenuMusicOnValue : MenuMusicOffValue;
        global[VolumeKey] = Volume;

        using StringWriter stringWriter = new();
        using JsonTextWriter writer = new(stringWriter);

        // Preserve original format by using tabs as indentation
        writer.Formatting = Formatting.Indented;
        writer.IndentChar = '\t';
        writer.Indentation = 1;

        JsonSerializer serializer = new();
        serializer.Serialize(writer, jObj);

        Directory.CreateDirectory(SettingsFilePath.Parent);
        File.WriteAllText(SettingsFilePath, stringWriter.ToString());

        return Task.CompletedTask;
    }

    protected override void SettingsPropertyChanged(string propertyName)
    {
        if (propertyName is 
            nameof(FullscreenMode) or 
            nameof(SelectedLanguageIndex) or 
            nameof(IsMenuMusicEnabled) or 
            nameof(Volume))
        {
            UnsavedChanges = true;
        }
    }

    #endregion
}