using System.IO;
using RayCarrot.RCP.Metro.Games.Components;
using RayCarrot.RCP.Metro.Games.Finder;
using RayCarrot.RCP.Metro.Games.RichPresence;
using RayCarrot.RCP.Metro.Games.Settings;
using RayCarrot.RCP.Metro.Games.SetupGame;
using RayCarrot.RCP.Metro.Games.Structure;
using RayCarrot.RCP.Metro.ModLoader.Modules.UbiArtLocalization;

namespace RayCarrot.RCP.Metro;

/// <summary>
/// The Rayman Origins (Win32) game descriptor
/// </summary>
public sealed class GameDescriptor_RaymanOrigins_Win32 : Win32GameDescriptor
{
    #region Private Constant Fields

    private const string SteamId = "207490";

    private const string UbisoftConnectGameId = "80";
    private const string UbisoftConnectProductId = "56c4948888a7e300458b47dc";

    #endregion

    #region Public Properties

    public override string GameId => "RaymanOrigins_Win32";
    public override string LegacyGameId => "RaymanOrigins";
    public override Game Game => Game.RaymanOrigins;
    public override GameCategory Category => GameCategory.Rayman;

    public override LocalizedString DisplayName => new ResourceLocString(nameof(Resources.RaymanOrigins_Win32_Title));
    public override string[] SearchKeywords => new[] { "ro" };
    public override DateTime ReleaseDate => new(2012, 03, 29);

    public override GameIconAsset Icon => GameIconAsset.RaymanOrigins;
    public override GameBannerAsset Banner => GameBannerAsset.RaymanOrigins;

    #endregion

    #region Protected Methods

    protected override void RegisterComponents(IGameComponentBuilder builder)
    {
        base.RegisterComponents(builder);

        builder.Register(new SteamGameClientComponent(SteamId));
        builder.Register(new UbisoftConnectGameClientComponent(UbisoftConnectGameId, UbisoftConnectProductId));

        builder.Register(new ProgressionManagersComponent(x => new GameProgressionManager_RaymanOrigins_Win32(x, "Rayman Origins")));
        builder.Register(new GameSettingsComponent(x => new UbiArtSettingsViewModel(x, AppFilePaths.RaymanOriginsRegistryKey)));
        builder.Register<OnGameAddedComponent, AddToJumpListOnGameAddedComponent>();
        builder.Register<BinaryGameModeComponent>(new UbiArtGameModeComponent(UbiArtGameMode.RaymanOrigins_PC));
        builder.Register<ArchiveComponent>(new UbiArtArchiveComponent());
        builder.Register(new UbiArtPathsComponent("GameData", null));
        builder.Register<LaunchArgumentsComponent, UbiArtLaunchArgumentsComponent>();

        builder.Register(new PCGamingWikiComponent("Rayman_Origins"));

        builder.Register(new GameBananaGameComponent(5986));
        builder.Register(new FilesModModuleExamplePaths(x => Path.GetFileName(x) switch
        {
            "bundle_PC.ipk" => "itf_cooked/pc/actor/playablecharacter/rayman/animation",
            "" => "GameData",
            _ => null,
        }));
        builder.Register(new ModModuleComponent(_ => new UbiArtLocalizationModule()));

        builder.Register(new DiscordRichPresenceComponent("Rayman Origins", "rayman_origins"));
        builder.Register(new RichPresenceManagerComponent((g, p) => new GameRichPresenceManager_RaymanOrigins_Win32(g, p)));

        builder.Register(new SetupGameActionComponent(_ => new RaymanOriginsCrashDumpsSetupGameAction()));
        builder.Register(new SetupGameActionComponent(_ => new HighQualityRaymanOriginsVideosSetupGameAction()));
        builder.Register(new SetupGameActionComponent(_ => new MinimumRaymanOriginsLoadingTimesSetupGameAction()));
        builder.Register(new SetupGameActionComponent(_ => new InvalidUbiArtResolutionSetupGameAction(AppFilePaths.RaymanOriginsRegistryKey)));
    }

    protected override ProgramInstallationStructure CreateStructure() => new DirectoryProgramInstallationStructure(new ProgramFileSystem(new ProgramPath[]
    {
        // Files
        new ProgramFilePath("Rayman Origins.exe", ProgramPathType.PrimaryExe, required: true),
        
        // Directories
        new ProgramDirectoryPath("GameData", ProgramPathType.Data, required: true),
    }));

    #endregion

    #region Public Methods

    public override IEnumerable<GameAddAction> GetAddActions() => new GameAddAction[]
    {
        new LocateDirectoryGameAddAction(this),
    };

    public override IEnumerable<GamePurchaseLink> GetPurchaseLinks() => new GamePurchaseLink[]
    {
        new(new ResourceLocString(nameof(Resources.GameDisplay_PurchaseGOG)), "https://www.gog.com/game/rayman_origins"),
        new(new ResourceLocString(nameof(Resources.GameDisplay_Steam)), SteamHelpers.GetStorePageURL(SteamId)),
        new(new ResourceLocString(nameof(Resources.GameDisplay_PurchaseUplay)), UbisoftConnectHelpers.GetStorePageURL(UbisoftConnectProductId)),
    };

    public override FinderQuery[] GetFinderQueries() => new FinderQuery[]
    {
        new UninstallProgramFinderQuery("Rayman Origins"),
        new UninstallProgramFinderQuery("Rayman: Origins"),

        new Win32ShortcutFinderQuery("Rayman Origins"),

        new SteamFinderQuery(SteamId),

        new UbisoftConnectFinderQuery(UbisoftConnectGameId),
    };

    #endregion
}