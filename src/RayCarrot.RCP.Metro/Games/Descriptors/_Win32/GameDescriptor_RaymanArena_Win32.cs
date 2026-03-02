using RayCarrot.RCP.Metro.Games.Components;
using RayCarrot.RCP.Metro.Games.Finder;
using RayCarrot.RCP.Metro.Games.Settings;
using RayCarrot.RCP.Metro.Games.Structure;
using RayCarrot.RCP.Metro.Ini;

namespace RayCarrot.RCP.Metro;

/// <summary>
/// The Rayman Arena (Win32) game descriptor
/// </summary>
public sealed class GameDescriptor_RaymanArena_Win32 : Win32GameDescriptor
{
    #region Public Properties

    public override string GameId => "RaymanArena_Win32";
    public override string LegacyGameId => "RaymanArena";
    public override Game Game => Game.RaymanMArena;
    public override GameCategory Category => GameCategory.Rayman;

    public override LocalizedString DisplayName => new ResourceLocString(nameof(Resources.RaymanArena_Win32_Title));
    public override string[] SearchKeywords => new[] { "ra" };
    public override DateTime ReleaseDate => new(2002, 09, 24);

    public override GameIconAsset Icon => GameIconAsset.RaymanArena;
    public override GameBannerAsset Banner => GameBannerAsset.RaymanM;

    #endregion

    #region Protected Methods

    protected override void RegisterComponents(IGameComponentBuilder builder)
    {
        base.RegisterComponents(builder);

        builder.Register(new ProgressionManagersComponent(x => new GameProgressionManager_RaymanMArena_Win32(x, "Rayman Arena", false)));
        builder.Register(new GameSettingsComponent(x => new RaymanArenaSettingsViewModel(x)));
        builder.Register<OnGameAddedComponent, AddToJumpListOnGameAddedComponent>();
        builder.Register<LocalGameLinksComponent, RaymanMArenaSetupLocalGameLinksComponent>();
        builder.Register(new PCGamingWikiComponent("Rayman_M"));
        builder.Register(new RayMapComponent(RayMapComponent.RayMapViewer.RayMap, "ra_pc", "ra_pc"));
        builder.Register<BinaryGameModeComponent>(new CPAGameModeComponent(CPAGameMode.RaymanArena_PC));
        builder.Register<ArchiveComponent>(new CPAArchiveComponent(_ => new[]
        {
            @"MenuBin\tex32.cnt",
            @"MenuBin\vignette.cnt",
            @"MenuBin\Sound.cnt",

            @"FishBin\tex32.cnt",
            @"FishBin\vignette.cnt",
            @"FishBin\Sound.cnt",

            @"TribeBin\tex32.cnt",
            @"TribeBin\vignette.cnt",
            @"TribeBin\Sound.cnt",
        }));
        builder.Register<CPATextureSyncComponent, RaymanMArenaCPATextureSyncComponent>();

        builder.Register(new GameBananaGameComponent(8592));
    }

    protected override ProgramInstallationStructure CreateStructure() => new DirectoryProgramInstallationStructure(new ProgramFileSystem(new ProgramPath[]
    {
        // Files
        new ProgramFilePath("R_Arena.exe", ProgramPathType.PrimaryExe, required: true),
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
            discFilesListFileName: "RaymanArena",
            gameLogo: GameLogoAsset.RaymanArena,
            gifFileNames: new[] { "ASTRO.gif", "CASK.gif", "CHASE.gif", "GLOB.gif", "RODEO.gif", },
            installFolderName: "Rayman Arena"))
    };

    public override FinderQuery[] GetFinderQueries() => new FinderQuery[]
    {
        new UbiIniFinderQuery(RaymanArenaIniAppData.SectionName),

        new UninstallProgramFinderQuery("Rayman Arena"),
        new UninstallProgramFinderQuery("Rayman: Arena"),

        new Win32ShortcutFinderQuery("Rayman Arena"),
    };

    #endregion
}