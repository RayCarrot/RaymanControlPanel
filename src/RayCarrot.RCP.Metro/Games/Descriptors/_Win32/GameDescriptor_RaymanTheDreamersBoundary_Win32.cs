using RayCarrot.RCP.Metro.Games.Components;
using RayCarrot.RCP.Metro.Games.Structure;

namespace RayCarrot.RCP.Metro;

/// <summary>
/// The Rayman: The Dreamer's Boundary (Win32) game descriptor
/// </summary>
public sealed class GameDescriptor_RaymanTheDreamersBoundary_Win32 : Win32GameDescriptor
{
    #region Constant Fields

    private const string GameJoltUrl = "https://gamejolt.com/games/dreamersboundary/507525";

    #endregion

    #region Public Properties

    public override string GameId => "RaymanTheDreamersBoundary_Win32";
    public override Game Game => Game.RaymanTheDreamersBoundary;
    public override GameCategory Category => GameCategory.Fan;

    public override LocalizedString DisplayName => new ResourceLocString(nameof(Resources.RaymanTheDreamersBoundary_Win32_Title));
    public override DateTime ReleaseDate => new(2022, 09, 15);

    public override GameIconAsset Icon => GameIconAsset.RaymanTheDreamersBoundary;
    public override GameBannerAsset Banner => GameBannerAsset.RaymanTheDreamersBoundary;

    #endregion

    #region Protected Methods

    protected override void RegisterComponents(IGameComponentBuilder builder)
    {
        base.RegisterComponents(builder);

        builder.Register(new ProgressionManagersComponent(x => new GameProgressionManager_RaymanTheDreamersBoundary_Win32(x, "Rayman The Dreamer's Boundary")));
        builder.Register<ExternalGameLinksComponent>(new GameJoltExternalGameLinksComponent(GameJoltUrl));

        builder.Register(new DiscordRichPresenceComponent("Rayman: The Dreamer's Boundary", "rayman_the_dreamers_boundary"));
    }
    
    protected override ProgramInstallationStructure CreateStructure() => new DirectoryProgramInstallationStructure(new ProgramFileSystem(new ProgramPath[]
    {
        // Files
        new ProgramFilePath("Rayman - The Dreamer's Boundary.exe", ProgramPathType.PrimaryExe, required: true),
    }));

    #endregion

    #region Public Methods

    public override IEnumerable<GameAddAction> GetAddActions() => new GameAddAction[]
    {
        new LocateDirectoryGameAddAction(this),
    };

    public override IEnumerable<GamePurchaseLink> GetPurchaseLinks() => new GamePurchaseLink[]
    {
        new(new ResourceLocString(nameof(Resources.GameDisplay_GameJolt)), GameJoltUrl, GenericIconKind.GameAction_Web),
    };

    #endregion
}