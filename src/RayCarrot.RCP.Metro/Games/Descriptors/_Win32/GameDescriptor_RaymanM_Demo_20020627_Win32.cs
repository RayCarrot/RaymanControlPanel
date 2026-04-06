using RayCarrot.RCP.Metro.Games.Components;
using RayCarrot.RCP.Metro.Games.Settings;
using RayCarrot.RCP.Metro.Games.Structure;

namespace RayCarrot.RCP.Metro;

/// <summary>
/// The Rayman M Demo 2002/06/27 (Win32) game descriptor
/// </summary>
public sealed class GameDescriptor_RaymanM_Demo_20020627_Win32 : Win32GameDescriptor
{
    #region Public Properties

    public override string GameId => "RaymanM_Demo_20020627_Win32";
    public override string LegacyGameId => "Demo_RaymanM";
    public override Game Game => Game.RaymanMArena;
    public override GameCategory Category => GameCategory.Rayman;
    public override GameType Type => GameType.Demo;

    public override LocalizedString DisplayName => new ResourceLocString(nameof(Resources.RaymanM_Demo_20020627_Win32_Title));
    public override DateTime ReleaseDate => new(2002, 06, 27);

    public override GameIconAsset Icon => GameIconAsset.RaymanM;
    public override GameBannerAsset Banner => GameBannerAsset.RaymanM;

    #endregion

    #region Protected Methods

    protected override void RegisterComponents(IGameComponentBuilder builder)
    {
        base.RegisterComponents(builder);

        builder.Register(new ProgressionManagersComponent(x => new GameProgressionManager_RaymanMArena_Win32(x, "Rayman M Demo", true)));
        builder.Register(new GameSettingsComponent(x => new RaymanMDemoSettingsViewModel(x)));
        builder.Register<LocalGameLinksComponent, RaymanMArenaSetupLocalGameLinksComponent>();
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

        builder.Register(new DiscordRichPresenceComponent("Rayman M Demo", "rayman_m"));
    }

    protected override ProgramInstallationStructure CreateStructure() => new DirectoryProgramInstallationStructure(new ProgramFileSystem(new ProgramPath[]
    {
        // Files
        new ProgramFilePath("RaymanM.exe", ProgramPathType.PrimaryExe, required: true),
    }));

    #endregion

    #region Public Methods

    public override IEnumerable<GameAddAction> GetAddActions() => new GameAddAction[]
    {
        new LocateDirectoryGameAddAction(this),
    };

    #endregion
}