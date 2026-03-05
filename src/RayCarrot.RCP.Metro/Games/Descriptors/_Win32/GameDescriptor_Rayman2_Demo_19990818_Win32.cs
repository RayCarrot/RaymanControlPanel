using RayCarrot.RCP.Metro.Games.Components;
using RayCarrot.RCP.Metro.Games.Structure;

namespace RayCarrot.RCP.Metro;

/// <summary>
/// The Rayman 2 Demo 1999/08/18 (Win32) game descriptor
/// </summary>
public sealed class GameDescriptor_Rayman2_Demo_19990818_Win32 : Win32GameDescriptor
{
    #region Public Properties

    public override string GameId => "Rayman2_Demo_19990818_Win32";
    public override string LegacyGameId => "Demo_Rayman2_1";
    public override Game Game => Game.Rayman2;
    public override GameCategory Category => GameCategory.Rayman;
    public override GameType Type => GameType.Demo;

    public override LocalizedString DisplayName => new ResourceLocString(nameof(Resources.Rayman2_Demo_19990818_Win32_Title));
    public override DateTime ReleaseDate => new(1999, 08, 18);

    public override GameIconAsset Icon => GameIconAsset.Rayman2_Demo;
    public override GameBannerAsset Banner => GameBannerAsset.Rayman2;

    #endregion

    #region Protected Methods

    protected override void RegisterComponents(IGameComponentBuilder builder)
    {
        base.RegisterComponents(builder);

        builder.Register<OnGameAddedComponent, SetupRayman2DemoAppDataOnGameAddedComponent>();
        builder.Register<OnGameAddedComponent, DefaultToRunAsAdminOnGameAddedComponent>();
        builder.Register<BinaryGameModeComponent>(new CPAGameModeComponent(CPAGameMode.Rayman2_Demo1_PC));
        builder.Register<ArchiveComponent>(new CPAArchiveComponent(_ => new[]
        {
            @"BinData\Textures.cnt",
        }));

        builder.Register(new DiscordRichPresenceComponent("Rayman 2 Demo", "rayman_2"));
    }

    protected override ProgramInstallationStructure CreateStructure() => new DirectoryProgramInstallationStructure(new ProgramFileSystem(new ProgramPath[]
    {
        // Files
        new ProgramFilePath("Rayman2Demo.exe", ProgramPathType.PrimaryExe, required: true),
    }));

    #endregion

    #region Public Methods

    public override IEnumerable<GameAddAction> GetAddActions() => new GameAddAction[]
    {
        new LocateDirectoryGameAddAction(this),
    };

    #endregion
}