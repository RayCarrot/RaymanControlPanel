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
/// The Rayman Legends (Win32) game descriptor
/// </summary>
public sealed class GameDescriptor_RaymanLegends_Win32 : Win32GameDescriptor
{
    #region Private Constant Fields

    private const string SteamId = "242550";

    private const string UbisoftConnectGameId = "410"; // NOTE: The demo has id 411
    private const string UbisoftConnectProductId = "56c4948888a7e300458b47da";

    #endregion

    #region Public Properties

    public override string GameId => "RaymanLegends_Win32";
    public override string LegacyGameId => "RaymanLegends";
    public override Game Game => Game.RaymanLegends;
    public override GameCategory Category => GameCategory.Rayman;

    public override LocalizedString DisplayName => new ResourceLocString(nameof(Resources.RaymanLegends_Win32_Title));
    public override string[] SearchKeywords => new[] { "rl" };
    public override DateTime ReleaseDate => new(2013, 08, 29);

    public override GameIconAsset Icon => GameIconAsset.RaymanLegends;
    public override GameBannerAsset Banner => GameBannerAsset.RaymanLegends;

    #endregion

    #region Protected Methods

    protected override void RegisterComponents(IGameComponentBuilder builder)
    {
        base.RegisterComponents(builder);

        builder.Register(new SteamGameClientComponent(SteamId));
        builder.Register(new UbisoftConnectGameClientComponent(UbisoftConnectGameId, UbisoftConnectProductId));

        builder.Register(new ProgressionManagersComponent(x => new GameProgressionManager_RaymanLegends_Win32(x, "Rayman Legends")));
        builder.Register(new GameSettingsComponent(x => new UbiArtSettingsViewModel(x, AppFilePaths.RaymanLegendsRegistryKey)));
        builder.Register<OnGameAddedComponent, AddToJumpListOnGameAddedComponent>();
        builder.Register<BinaryGameModeComponent>(new UbiArtGameModeComponent(UbiArtGameMode.RaymanLegends_PC));
        builder.Register<ArchiveComponent>(new UbiArtArchiveComponent());
        builder.Register(new UbiArtPathsComponent(String.Empty, "secure_fat.gf"));
        builder.Register<LaunchArgumentsComponent, UbiArtLaunchArgumentsComponent>();

        builder.Register(new PCGamingWikiComponent("Rayman_Legends"));

        builder.Register(new GameBananaGameComponent(7400));
        builder.Register(new FilesModModuleExamplePaths(x => Path.GetFileName(x) switch
        {
            "Bundle_PC.ipk" => "cache/itf_cooked/pc/world/common/playablecharacter/rayman/animation",
            "persistentLoading_PC.ipk" => "cache/itf_cooked/pc/world/common/playablecharacter/drcplayer/animation",
            "" => "video",
            _ => null,
        }));
        builder.Register(new ModModuleComponent(_ => new UbiArtLocalizationModule()));

        builder.Register(new SetupGameActionComponent(_ => new InvalidUbiArtResolutionSetupGameAction(AppFilePaths.RaymanLegendsRegistryKey)));

        builder.Register(new DiscordRichPresenceComponent("Rayman Legends", "rayman_legends"));
        builder.Register(new RichPresenceManagerComponent((g, p) => new GameRichPresenceManager_RaymanLegends_Win32(g, p)));
    }

    protected override ProgramInstallationStructure CreateStructure() => new DirectoryProgramInstallationStructure(new ProgramFileSystem(new ProgramPath[]
    {
        // Files
        new ProgramFilePath("Rayman Legends.exe", ProgramPathType.PrimaryExe, required: true),
        new ProgramFilePath("Bundle_PC.ipk", ProgramPathType.Data, required: true),
    }));

    #endregion

    #region Public Methods

    public override IEnumerable<GameAddAction> GetAddActions() => new GameAddAction[]
    {
        new LocateDirectoryGameAddAction(this),
    };

    public override IEnumerable<GamePurchaseLink> GetPurchaseLinks() => new GamePurchaseLink[]
    {
        new(new ResourceLocString(nameof(Resources.GameDisplay_Steam)), SteamHelpers.GetStorePageURL(SteamId)),
        new(new ResourceLocString(nameof(Resources.GameDisplay_PurchaseUplay)), UbisoftConnectHelpers.GetStorePageURL(UbisoftConnectProductId)),

    };

    public override FinderQuery[] GetFinderQueries() => new FinderQuery[]
    {
        new UninstallProgramFinderQuery("Rayman Legends"),
        new UninstallProgramFinderQuery("Rayman: Legends"),

        new Win32ShortcutFinderQuery("Rayman Legends"),

        new SteamFinderQuery(SteamId),

        new UbisoftConnectFinderQuery(UbisoftConnectGameId),
    };

    #endregion
}