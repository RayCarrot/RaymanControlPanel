using RayCarrot.RCP.Metro.Games.Components;
using RayCarrot.RCP.Metro.Games.Structure;

namespace RayCarrot.RCP.Metro;

/// <summary>
/// The Rayman Garden PLUS (Win32) game descriptor
/// </summary>
public sealed class GameDescriptor_RaymanGardenPLUS_Win32 : Win32GameDescriptor
{
    #region Constant Fields

    private const string GameJoltUrl = "https://gamejolt.com/games/RaymanGardenPlus/622289";

    #endregion

    #region Public Properties

    public override string GameId => "RaymanGardenPLUS_Win32";
    public override string LegacyGameId => "RaymanGardenPLUS";
    public override Game Game => Game.RaymanGardenPLUS;
    public override GameCategory Category => GameCategory.Fan;

    public override LocalizedString DisplayName => new ResourceLocString(nameof(Resources.RaymanGardenPLUS_Win32_Title));
    public override DateTime ReleaseDate => new(2021, 06, 05);

    public override GameIconAsset Icon => GameIconAsset.RaymanGardenPLUS;
    public override GameBannerAsset Banner => GameBannerAsset.RaymanGardenPLUS;

    #endregion

    #region Protected Methods

    protected override void RegisterComponents(IGameComponentBuilder builder)
    {
        base.RegisterComponents(builder);

        builder.Register<Win32LaunchPathComponent, SingleExeWin32LaunchPathComponent>();

        builder.Register<ExternalGameLinksComponent>(new GameJoltExternalGameLinksComponent(GameJoltUrl));

        builder.Register(new DiscordRichPresenceComponent("Rayman Garden PLUS", "rayman_garden_plus"));
    }

    protected override ProgramInstallationStructure CreateStructure() => new ExeProgramInstallationStructure();

    #endregion

    #region Public Methods

    public override IEnumerable<GameAddAction> GetAddActions() => new GameAddAction[]
    {
        new LocateFileGameAddAction(this),
    };

    public override IEnumerable<GamePurchaseLink> GetPurchaseLinks() => new GamePurchaseLink[]
    {
        new(new ResourceLocString(nameof(Resources.GameDisplay_GameJolt)), GameJoltUrl, GenericIconKind.GameAction_Web),
    };

    #endregion
}