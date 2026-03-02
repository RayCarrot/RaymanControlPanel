using RayCarrot.RCP.Metro.Games.Components;
using RayCarrot.RCP.Metro.Games.Finder;
using RayCarrot.RCP.Metro.Games.Settings;
using RayCarrot.RCP.Metro.Games.Structure;

namespace RayCarrot.RCP.Metro;

/// <summary>
/// The Rayman Raving Rabbids (Win32) game descriptor
/// </summary>
public sealed class GameDescriptor_RaymanRavingRabbids_Win32 : Win32GameDescriptor
{
    #region Private Constant Fields

    private const string SteamId = "15080";

    private const string UbisoftConnectGameId = "362";
    private const string UbisoftConnectProductId = "56c4948888a7e300458b47de";

    #endregion

    #region Public Properties

    public override string GameId => "RaymanRavingRabbids_Win32";
    public override string LegacyGameId => "RaymanRavingRabbids";
    public override Game Game => Game.RaymanRavingRabbids;
    public override GameCategory Category => GameCategory.Rabbids;

    public override LocalizedString DisplayName => new ResourceLocString(nameof(Resources.RaymanRavingRabbids_Win32_Title));
    public override string[] SearchKeywords => new[] { "rrr" };
    public override DateTime ReleaseDate => new(2006, 12, 07);

    public override GameIconAsset Icon => GameIconAsset.RaymanRavingRabbids;
    public override GameBannerAsset Banner => GameBannerAsset.RaymanRavingRabbids;

    #endregion

    #region Private Methods

    private static IEnumerable<GameLinksComponent.GameUriLink> GetLocalGameLinks(GameInstallation gameInstallation) => new[]
    {
        new GameLinksComponent.GameUriLink(
            Header: new ResourceLocString(nameof(Resources.GameLink_Setup)), 
            Uri: gameInstallation.InstallLocation.Directory + "SettingsApplication.exe")
    };

    #endregion

    #region Protected Methods

    protected override void RegisterComponents(IGameComponentBuilder builder)
    {
        base.RegisterComponents(builder);

        builder.Register(new SteamGameClientComponent(SteamId));
        builder.Register(new UbisoftConnectGameClientComponent(UbisoftConnectGameId, UbisoftConnectProductId));

        builder.Register(new ProgressionManagersComponent(x => new GameProgressionManager_RaymanRavingRabbids_Win32(x, "Rayman Raving Rabbids")));
        builder.Register(new GameSettingsComponent(x => new RaymanRavingRabbidsSettingsViewModel(x)));
        builder.Register<OnGameAddedComponent, AddToJumpListOnGameAddedComponent>();
        // TODO: Launch game exe directly and allow custom args like for RGH?
        builder.Register(new Win32LaunchPathComponent(x => x.InstallLocation.Directory + "CheckApplication.exe"));
        builder.Register(new LocalGameLinksComponent(GetLocalGameLinks));
        builder.Register<BinaryGameModeComponent>(new JadeGameModeComponent(JadeGameMode.RaymanRavingRabbids_PC));
        builder.Register<PrototypeRestorationComponent>();

        builder.Register(new PCGamingWikiComponent("Rayman_Raving_Rabbids"));

        builder.Register(new GameBananaGameComponent(18586));
    }

    protected override ProgramInstallationStructure CreateStructure() => new DirectoryProgramInstallationStructure(new ProgramFileSystem(new ProgramPath[]
    {
        // Files
        new ProgramFilePath("Jade_enr.exe", ProgramPathType.PrimaryExe, required: true),
        new ProgramFilePath("CheckApplication.exe", ProgramPathType.OtherExe, required: true),
    }));

    #endregion

    #region Public Methods

    public override IEnumerable<GameAddAction> GetAddActions() => new GameAddAction[]
    {
        new LocateDirectoryGameAddAction(this),
    };

    public override IEnumerable<GamePurchaseLink> GetPurchaseLinks() => new GamePurchaseLink[]
    {
        new(new ResourceLocString(nameof(Resources.GameDisplay_PurchaseGOG)), "https://www.gog.com/game/rayman_raving_rabbids"),
        new(new ResourceLocString(nameof(Resources.GameDisplay_Steam)), SteamHelpers.GetStorePageURL(SteamId)),
        new(new ResourceLocString(nameof(Resources.GameDisplay_PurchaseUplay)), UbisoftConnectHelpers.GetStorePageURL(UbisoftConnectProductId)),
    };

    public override FinderQuery[] GetFinderQueries() => new FinderQuery[]
    {
        new UninstallProgramFinderQuery("Rayman Raving Rabbids"),
        new UninstallProgramFinderQuery("Rayman: Raving Rabbids"),
        new UninstallProgramFinderQuery("RRR"),

        new Win32ShortcutFinderQuery("Rayman Raving Rabbids"),

        new SteamFinderQuery(SteamId),

        new UbisoftConnectFinderQuery(UbisoftConnectGameId),
    };

    #endregion
}