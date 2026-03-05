using RayCarrot.RCP.Metro.Ini;

namespace RayCarrot.RCP.Metro.Games.Settings;

/// <summary>
/// View model for the Rayman Arena configuration
/// </summary>
public class RaymanArenaSettingsViewModel : BaseRayman3MArenaSettingsViewModel<RaymanArenaIniAppData, RaymanArenaLanguage>
{
    #region Constructor

    public RaymanArenaSettingsViewModel(GameInstallation gameInstallation) : base(gameInstallation) { }

    #endregion

    #region Protected Properties

    protected override FilePatcher_Patch[] RemoveDiscCheckPatches => new[]
    {
        new FilePatcher_Patch(0x22D000, new[]
        {
            new FilePatcher_Patch.PatchEntry(
                PatchOffset: 0x2FAE6, 
                OriginalBytes: new byte[]
                {
                    0x05, 0x75, 0x3F, 0x6A, 0x00, 0x6A, 0x00, 0x6A, 
                    0x00, 0x6A, 0x00, 0x6A, 0x00, 0x8D, 0x94, 0x24, 
                    0x30, 0x02, 0x00, 0x00, 0x68, 0x80, 0x00, 0x00, 
                    0x00, 0x8D, 0x44, 0x24, 0x28, 0x52, 0x50, 0xFF, 
                    0x15, 0xD0, 0xD0, 0x5A, 0x00, 0x8D, 0x8C, 0x24, 
                    0x1C, 0x02, 0x00, 0x00, 0x68, 0x94, 0xBE, 0x5D,
                    0x00, 0x51, 0xE8, 0x3B, 0x7A, 0x09, 0x00, 0x83, 
                    0xC4, 0x08, 0x85, 0xC0, 0x0F, 0x84, 0x49, 0x01,
                    0x00, 0x00, 0x43, 0x83, 0xFB, 0x20, 0x7C, 0x96,
                }, 
                PatchRevisions: new[]
                {
                    new FilePatcher_Patch.PatchedBytesRevision(0, new byte[]
                    {
                        0x05, 0x75, 0x3F, 0x6A, 0x00, 0x6A, 0x00, 0x6A,
                        0x00, 0x6A, 0x00, 0x6A, 0x00, 0x8D, 0x94, 0x24,
                        0x30, 0x02, 0x00, 0x00, 0x68, 0x80, 0x00, 0x00,
                        0x00, 0x8D, 0x44, 0x24, 0x28, 0x52, 0x50, 0xFF,
                        0x15, 0xD0, 0xD0, 0x5A, 0x00, 0x8D, 0x8C, 0x24,
                        0x1C, 0x02, 0x00, 0x00, 0x68, 0x94, 0xBE, 0x5D,
                        0x00, 0x51, 0xE8, 0x3B, 0x7A, 0x09, 0x00, 0x83,
                        0xC4, 0x08, 0x85, 0xC0, 0xE9, 0x4A, 0x01, 0x00, // Return even if label doesn't match
                        0x00, 0x00, 0x43, 0x83, 0xFB, 0x20, 0x7C, 0x96,
                    }),
                    new FilePatcher_Patch.PatchedBytesRevision(1, new byte[]
                    {
                        0x03, 0x75, 0x3F, 0x6A, 0x00, 0x6A, 0x00, 0x6A, // Check for DRIVE_FIXED instead of DRIVE_CDROM to work without disc drives
                        0x00, 0x6A, 0x00, 0x6A, 0x00, 0x8D, 0x94, 0x24,
                        0x30, 0x02, 0x00, 0x00, 0x68, 0x80, 0x00, 0x00,
                        0x00, 0x8D, 0x44, 0x24, 0x28, 0x52, 0x50, 0xFF,
                        0x15, 0xD0, 0xD0, 0x5A, 0x00, 0x8D, 0x8C, 0x24,
                        0x1C, 0x02, 0x00, 0x00, 0x68, 0x94, 0xBE, 0x5D,
                        0x00, 0x51, 0xE8, 0x3B, 0x7A, 0x09, 0x00, 0x83,
                        0xC4, 0x08, 0x85, 0xC0, 0xE9, 0x4A, 0x01, 0x00, // Return even if label doesn't match
                        0x00, 0x00, 0x43, 0x83, 0xFB, 0x20, 0x7C, 0x96,
                    }),
                })
        }),
    };

    #endregion

    #region Public Properties

    public override bool HasControllerConfig => false;
    public override bool HasNetworkConfig => true;
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
        CurrentLanguage = Enum.TryParse(AppDataManager.AppData.Language, out RaymanArenaLanguage lang) ? lang : RaymanArenaLanguage.English;
        ModemQualityIndex = AppDataManager.AppData.ModemQuality;
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
        AppDataManager.AppData.ModemQuality = ModemQualityIndex;
        AppDataManager.AppData.TexturesFile = $"Tex{(IsTextures32Bit ? 32 : 16)}.cnt";

        SaveMArenaSaveFile();
    }

    #endregion
}