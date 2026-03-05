using RayCarrot.RCP.Metro.Games.Components;
using RayCarrot.RCP.Metro.Games.Structure;

namespace RayCarrot.RCP.Metro;

/// <summary>
/// The Rayman Redemption (Win32) game descriptor
/// </summary>
public sealed class GameDescriptor_RaymanRedemption_Win32 : Win32GameDescriptor
{
    #region Constant Fields

    private const string GameJoltUrl = "https://gamejolt.com/games/raymanredemption/340532";

    #endregion

    #region Public Properties

    public override string GameId => "RaymanRedemption_Win32";
    public override string LegacyGameId => "RaymanRedemption";
    public override Game Game => Game.RaymanRedemption;
    public override GameCategory Category => GameCategory.Fan;

    public override LocalizedString DisplayName => new ResourceLocString(nameof(Resources.RaymanRedemption_Win32_Title));
    public override DateTime ReleaseDate => new(2020, 06, 19);

    public override GameIconAsset Icon => GameIconAsset.RaymanRedemption;
    public override GameBannerAsset Banner => GameBannerAsset.RaymanRedemption;

    #endregion

    #region Protected Methods

    protected override void RegisterComponents(IGameComponentBuilder builder)
    {
        base.RegisterComponents(builder);

        builder.Register(new ProgressionManagersComponent(x => new GameProgressionManager_RaymanRedemption_Win32(x, "Rayman Redemption")));
        builder.Register<ExternalGameLinksComponent>(new GameJoltExternalGameLinksComponent(GameJoltUrl));

        builder.Register(new PCGamingWikiComponent("Rayman_Redemption"));

        builder.Register(new GameBananaGameComponent(20873));

        builder.Register(new DiscordRichPresenceComponent("Rayman Redemption", "rayman_redemption"));
    }
    
    protected override ProgramInstallationStructure CreateStructure() => new DirectoryProgramInstallationStructure(new ProgramFileSystem(new ProgramPath[]
    {
        // Files
        new ProgramFilePath("Rayman Redemption.exe", ProgramPathType.PrimaryExe, required: true),
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