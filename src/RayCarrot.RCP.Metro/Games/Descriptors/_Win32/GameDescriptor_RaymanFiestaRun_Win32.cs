using RayCarrot.RCP.Metro.Games.Components;
using RayCarrot.RCP.Metro.Games.Finder;
using RayCarrot.RCP.Metro.Games.Settings;
using RayCarrot.RCP.Metro.Games.Structure;

namespace RayCarrot.RCP.Metro;

/// <summary>
/// The Rayman Fiesta Run (Win32) game descriptor
/// </summary>
public sealed class GameDescriptor_RaymanFiestaRun_Win32 : Win32GameDescriptor
{
    #region Private Constant Fields

    private const string UbisoftConnectGameId = "5860";
    private const string UbisoftConnectProductId = "62c7e336a2d84f27e54670a6";

    #endregion

    #region Public Properties

    public override string GameId => "RaymanFiestaRun_Win32";
    public override Game Game => Game.RaymanFiestaRun;
    public override GameCategory Category => GameCategory.Rayman;

    public override LocalizedString DisplayName => new ResourceLocString(nameof(Resources.RaymanFiestaRun_Win32_Title));
    public override string[] SearchKeywords => new[] { "rfr" };
    public override DateTime ReleaseDate => new(2022, 12, 22);

    public override GameIconAsset Icon => GameIconAsset.RaymanFiestaRun;
    public override GameBannerAsset Banner => GameBannerAsset.RaymanFiestaRun;

    #endregion

    #region Protected Methods

    protected override void RegisterComponents(IGameComponentBuilder builder)
    {
        base.RegisterComponents(builder);

        builder.Register(new UbisoftConnectGameClientComponent(UbisoftConnectGameId, UbisoftConnectProductId));

        builder.Register(new ProgressionManagersComponent(x => new GameProgressionManager_RaymanFiestaRun_Win32(x, "Rayman Fiesta Run (Ubisoft Connect)")));
        builder.Register(new GameSettingsComponent(x => new RaymanFiestaRunSettingsViewModel(x, UbisoftConnectHelpers.GetSaveDirectory(x), true)));
        builder.Register<OnGameAddedComponent, AddToJumpListOnGameAddedComponent>();
        builder.Register<BinaryGameModeComponent>(new UbiArtGameModeComponent(UbiArtGameMode.RaymanFiestaRun_PC));

        builder.Register(new PCGamingWikiComponent("Rayman_Fiesta_Run"));

        builder.Register(new GameBananaGameComponent(19729));
    }

    protected override ProgramInstallationStructure CreateStructure() => new DirectoryProgramInstallationStructure(new ProgramFileSystem(new ProgramPath[]
    {
        // Files
        new ProgramFilePath("Rayman Fiesta Run.exe", ProgramPathType.PrimaryExe, required: true),
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
        new UninstallProgramFinderQuery("Rayman Fiesta Run"),

        new Win32ShortcutFinderQuery("Rayman Fiesta Run"),

        new UbisoftConnectFinderQuery(UbisoftConnectGameId),
    };

    #endregion
}