using RayCarrot.RCP.Metro.Games.Components;
using RayCarrot.RCP.Metro.Games.Finder;
using RayCarrot.RCP.Metro.Games.Settings;
using RayCarrot.RCP.Metro.Games.Structure;

namespace RayCarrot.RCP.Metro;

/// <summary>
/// The Rayman Jungle Run (Win32) game descriptor
/// </summary>
public sealed class GameDescriptor_RaymanJungleRun_Win32 : Win32GameDescriptor
{
    #region Private Constant Fields

    private const string UbisoftConnectGameId = "5850";
    private const string UbisoftConnectProductId = "62c7e362a2d84f27e54670a7";

    #endregion

    #region Public Properties

    public override string GameId => "RaymanJungleRun_Win32";
    public override Game Game => Game.RaymanJungleRun;
    public override GameCategory Category => GameCategory.Rayman;

    public override LocalizedString DisplayName => new ResourceLocString(nameof(Resources.RaymanJungleRun_Win32_Title));
    public override string[] SearchKeywords => new[] { "rjr" };
    public override DateTime ReleaseDate => new(2022, 12, 22);

    public override GameIconAsset Icon => GameIconAsset.RaymanJungleRun;
    public override GameBannerAsset Banner => GameBannerAsset.RaymanJungleRun;

    #endregion

    #region Protected Methods

    protected override void RegisterComponents(IGameComponentBuilder builder)
    {
        base.RegisterComponents(builder);

        builder.Register(new UbisoftConnectGameClientComponent(UbisoftConnectGameId, UbisoftConnectProductId));

        builder.Register(new ProgressionManagersComponent(x => new GameProgressionManager_RaymanJungleRun_Win32(x, "Rayman Jungle Run (Ubisoft Connect)")));
        builder.Register(new GameSettingsComponent(x => new RaymanJungleRunSettingsViewModel(x, UbisoftConnectHelpers.GetSaveDirectory(x), false, false, true)));
        builder.Register<OnGameAddedComponent, AddToJumpListOnGameAddedComponent>();
        builder.Register<BinaryGameModeComponent>(new UbiArtGameModeComponent(UbiArtGameMode.RaymanJungleRun_PC));

        builder.Register(new PCGamingWikiComponent("Rayman_Jungle_Run"));

        builder.Register(new GameBananaGameComponent(19728));
    }

    protected override ProgramInstallationStructure CreateStructure() => new DirectoryProgramInstallationStructure(new ProgramFileSystem(new ProgramPath[]
    {
        // Files
        new ProgramFilePath("Rayman Jungle Run.exe", ProgramPathType.PrimaryExe, required: true),
    }));

    #endregion

    #region Public Methods

    public override IEnumerable<GameAddAction> GetAddActions() => new GameAddAction[]
    {
        new LocateDirectoryGameAddAction(this),
    };

    public override IEnumerable<GamePurchaseLink> GetPurchaseLinks() => new GamePurchaseLink[]
    {
        new(new ResourceLocString(nameof(Resources.GameDisplay_PurchaseUplay)), UbisoftConnectHelpers.GetStorePageURL(UbisoftConnectProductId))
    };

    public override FinderQuery[] GetFinderQueries() => new FinderQuery[]
    {
        new UninstallProgramFinderQuery("Rayman Jungle Run"),

        new Win32ShortcutFinderQuery("Rayman Jungle Run"),

        new UbisoftConnectFinderQuery(UbisoftConnectGameId),
    };

    #endregion
}