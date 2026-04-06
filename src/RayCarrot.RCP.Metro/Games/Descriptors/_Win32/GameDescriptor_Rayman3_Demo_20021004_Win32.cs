using RayCarrot.RCP.Metro.Games.Components;
using RayCarrot.RCP.Metro.Games.Settings;
using RayCarrot.RCP.Metro.Games.Structure;

namespace RayCarrot.RCP.Metro;

/// <summary>
/// The Rayman 3 Demo 2002/10/04 (Win32) game descriptor
/// </summary>
public sealed class GameDescriptor_Rayman3_Demo_20021004_Win32 : Win32GameDescriptor
{
    #region Public Properties

    public override string GameId => "Rayman3_Demo_20021004_Win32";
    public override string LegacyGameId => "Demo_Rayman3_1";
    public override Game Game => Game.Rayman3;
    public override GameCategory Category => GameCategory.Rayman;
    public override GameType Type => GameType.Demo;

    public override LocalizedString DisplayName => new ResourceLocString(nameof(Resources.Rayman3_Demo_20021004_Win32_Title));
    public override DateTime ReleaseDate => new(2002, 10, 04);

    public override GameIconAsset Icon => GameIconAsset.Rayman3;
    public override GameBannerAsset Banner => GameBannerAsset.Rayman3;

    #endregion

    #region Protected Methods

    protected override void RegisterComponents(IGameComponentBuilder builder)
    {
        base.RegisterComponents(builder);

        builder.Register(new GameSettingsComponent(x => new Rayman3SettingsViewModel(x)));
        builder.Register<LocalGameLinksComponent>(new Rayman3SetupLocalGameLinksComponent(true));
        builder.Register<BinaryGameModeComponent>(new CPAGameModeComponent(CPAGameMode.Rayman3_PC));
        builder.Register<ArchiveComponent>(new CPAArchiveComponent(_ => new[]
        {
            @"Gamedatabin\tex32.cnt",
            @"Gamedatabin\vignette.cnt",
        }));
        builder.Register<CPATextureSyncComponent, Rayman3DemoCPATextureSyncComponent>();

        builder.Register(new DiscordRichPresenceComponent("Rayman 3 Demo", "rayman_3"));
    }

    protected override ProgramInstallationStructure CreateStructure() => new DirectoryProgramInstallationStructure(new ProgramFileSystem(new ProgramPath[]
    {
        // Files
        new ProgramFilePath("MainP5Pvf.exe", ProgramPathType.PrimaryExe, required: true),
    }));

    #endregion

    #region Public Methods

    public override IEnumerable<GameAddAction> GetAddActions() => new GameAddAction[]
    {
        new LocateDirectoryGameAddAction(this),
    };

    #endregion
}