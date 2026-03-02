using RayCarrot.RCP.Metro.Games.Components;
using RayCarrot.RCP.Metro.Games.Finder;
using RayCarrot.RCP.Metro.Games.Settings;
using RayCarrot.RCP.Metro.Games.Structure;
using RayCarrot.RCP.Metro.Ini;

namespace RayCarrot.RCP.Metro;

/// <summary>
/// The Rayman M (Win32) game descriptor
/// </summary>
public sealed class GameDescriptor_RaymanM_Win32 : Win32GameDescriptor
{
    #region Public Properties

    public override string GameId => "RaymanM_Win32";
    public override string LegacyGameId => "RaymanM";
    public override Game Game => Game.RaymanMArena;
    public override GameCategory Category => GameCategory.Rayman;

    public override LocalizedString DisplayName => new ResourceLocString(nameof(Resources.RaymanM_Win32_Title));
    public override string[] SearchKeywords => new[] { "rm" };
    public override DateTime ReleaseDate => new(2001, 11, 30);

    public override GameIconAsset Icon => GameIconAsset.RaymanM;
    public override GameBannerAsset Banner => GameBannerAsset.RaymanM;

    #endregion

    #region Protected Methods

    protected override void RegisterComponents(IGameComponentBuilder builder)
    {
        base.RegisterComponents(builder);

        builder.Register(new ProgressionManagersComponent(x => new GameProgressionManager_RaymanMArena_Win32(x, "Rayman M", false)));
        builder.Register(new GameSettingsComponent(x => new RaymanMSettingsViewModel(x)));
        builder.Register<OnGameAddedComponent, AddToJumpListOnGameAddedComponent>();
        builder.Register<LocalGameLinksComponent, RaymanMArenaSetupLocalGameLinksComponent>();
        builder.Register(new PCGamingWikiComponent("Rayman_M"));
        builder.Register(new RayMapComponent(RayMapComponent.RayMapViewer.RayMap, "rm_pc", "rm_pc"));
        builder.Register<BinaryGameModeComponent>(new CPAGameModeComponent(CPAGameMode.RaymanM_PC));
        builder.Register<ArchiveComponent>(new CPAArchiveComponent(_ => new[]
        {
            @"FishBin\tex32.cnt",
            @"FishBin\vignette.cnt",
            @"MenuBin\tex32.cnt",
            @"MenuBin\vignette.cnt",
            @"TribeBin\tex32.cnt",
            @"TribeBin\vignette.cnt",
        }));
        builder.Register<CPATextureSyncComponent, RaymanMArenaCPATextureSyncComponent>();
    }

    protected override ProgramInstallationStructure CreateStructure() => new DirectoryProgramInstallationStructure(new ProgramFileSystem(new ProgramPath[]
    {
        // Files
        new ProgramFilePath("RaymanM.exe", ProgramPathType.PrimaryExe, required: true),
        new ProgramFilePath("RM_Setup_DX8.exe", ProgramPathType.ConfigExe),

        // Directories
        new ProgramDirectoryPath("MenuBin", ProgramPathType.Data, required: true),
        new ProgramDirectoryPath("FishBin", ProgramPathType.Data, required: true),
        new ProgramDirectoryPath("TribeBin", ProgramPathType.Data, required: true),
        new ProgramDirectoryPath("MENU", ProgramPathType.Save),
    }));

    #endregion

    #region Public Methods

    public override IEnumerable<GameAddAction> GetAddActions() => new GameAddAction[]
    {
        new LocateDirectoryGameAddAction(this),
        new DiscInstallGameAddAction(this, new GameInstallerInfo(
            discFilesListFileName: "RaymanM",
            gameLogo: GameLogoAsset.RaymanM,
            gifFileNames: new[] { "ASTRO.gif", "CASK.gif", "CHASE.gif", "GLOB.gif", "RODEO.gif", },
            installFolderName: "Rayman M"))
    };

    public override FinderQuery[] GetFinderQueries() => new FinderQuery[]
    {
        new UbiIniFinderQuery(RaymanMIniAppData.SectionName),

        new UninstallProgramFinderQuery("Rayman M"),
        new UninstallProgramFinderQuery("Rayman: M"),

        new Win32ShortcutFinderQuery("Rayman M"),
    };

    #endregion
}