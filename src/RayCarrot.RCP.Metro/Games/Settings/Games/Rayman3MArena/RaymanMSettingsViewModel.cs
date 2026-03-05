using RayCarrot.RCP.Metro.Ini;

namespace RayCarrot.RCP.Metro.Games.Settings;

/// <summary>
/// View model for the Rayman M configuration
/// </summary>
public class RaymanMSettingsViewModel : BaseRayman3MArenaSettingsViewModel<RaymanMIniAppData, RaymanMLanguage>
{
    #region Constructor

    public RaymanMSettingsViewModel(GameInstallation gameInstallation) : base(gameInstallation) { }

    #endregion

    #region Protected Properties

    protected override FilePatcher_Patch[]? RemoveDiscCheckPatches => new[]
    {
        new FilePatcher_Patch(0x1C4000, new[]
        {
            new FilePatcher_Patch.PatchEntry(0x263D, new byte[]
            {
                0xE8,
                0x3E,
                0xBE,
                0x02,
                0x00,
            }, new byte[]
            {
                0xE9,
                0x00,
                0x00,
                0x00,
                0x00,
            })
        }), 
        new FilePatcher_Patch(0x1C3000, new[]
        {
            new FilePatcher_Patch.PatchEntry(0x21EE, new byte[]
            {
                0xE8,
                0xEd,
                0xBD,
                0x02,
                0x00,
            }, new byte[]
            {
                0xE9,
                0x00,
                0x00,
                0x00,
                0x00,
            })
        }), 
    };

    #endregion

    #region Public Properties

    public override bool HasControllerConfig => false;
    public override bool HasNetworkConfig => false;
    public override bool Has16BitTextures => true;

    #endregion

    #region Protected Methods

    protected override void LoadAppData()
    {
        if (CpaDisplayMode.TryParse(AppDataManager.AppData.GLI_Mode, out CpaDisplayMode displayMode))
        {
            GraphicsMode.SelectedGraphicsMode = new GraphicsMode(displayMode.Width, displayMode.Height);
            FullscreenMode = displayMode.IsFullscreen;
        }
        else
        {
            GraphicsMode.SelectedGraphicsMode = new GraphicsMode(800, 600);
            FullscreenMode = true;
        }

        TriLinear = AppDataManager.AppData.TriLinear != 0;
        TnL = AppDataManager.AppData.TnL != 0;
        CompressedTextures = AppDataManager.AppData.TexturesCompressed != 0;
        VideoQuality = AppDataManager.AppData.Video_WantedQuality;
        AutoVideoQuality = AppDataManager.AppData.Video_AutoAdjustQuality != 0;
        IsVideo32Bpp = AppDataManager.AppData.Video_BPP != 16;
        CurrentLanguage = Enum.TryParse(AppDataManager.AppData.Language, out RaymanMLanguage lang) ? lang : RaymanMLanguage.English;
        IsTextures32Bit = AppDataManager.AppData.TexturesFile == "Tex32.cnt";

        LoadMArenaSaveFile();
    }

    protected override void SaveAppData()
    {
        AppDataManager.AppData.GLI_Mode = new CpaDisplayMode()
        {
            BitsPerPixel = 32, // Depends on the graphics mode, but we assume it's always going to be 32
            IsFullscreen = FullscreenMode,
            Width = GraphicsMode.Width,
            Height = GraphicsMode.Height,
        }.ToString();

        AppDataManager.AppData.TriLinear = TriLinear ? 1 : 0;
        AppDataManager.AppData.TnL = TnL ? 1 : 0;
        AppDataManager.AppData.TexturesCompressed = CompressedTextures ? 1 : 0;
        AppDataManager.AppData.Video_WantedQuality = VideoQuality;
        AppDataManager.AppData.Video_AutoAdjustQuality = AutoVideoQuality ? 1 : 0;
        AppDataManager.AppData.Video_BPP = IsVideo32Bpp ? 32 : 16;
        AppDataManager.AppData.Language = CurrentLanguage.ToString();
        AppDataManager.AppData.TexturesFile = $"Tex{(IsTextures32Bit ? 32 : 16)}.cnt";

        SaveMArenaSaveFile();
    }

    #endregion
}