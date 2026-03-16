using System.IO;
using RayCarrot.RCP.Metro.Archive.CPA;
using RayCarrot.RCP.Metro.Games.Components;
using RayCarrot.RCP.Metro.Games.Finder;
using RayCarrot.RCP.Metro.Games.RichPresence;
using RayCarrot.RCP.Metro.Games.Settings;
using RayCarrot.RCP.Metro.Games.SetupGame;
using RayCarrot.RCP.Metro.Games.Structure;
using RayCarrot.RCP.Metro.Games.Tools.RuntimeModifications;
using RayCarrot.RCP.Metro.Ini;

namespace RayCarrot.RCP.Metro;

/// <summary>
/// The Rayman 3 (Win32) game descriptor
/// </summary>
public sealed class GameDescriptor_Rayman3_Win32 : Win32GameDescriptor
{
    #region Private Constant Fields

    private const string UbisoftConnectGameId = "360";
    private const string UbisoftConnectProductId = "5800b15eef3aa5ab3e8b4567";

    #endregion

    #region Public Properties

    public override string GameId => "Rayman3_Win32";
    public override string LegacyGameId => "Rayman3";
    public override Game Game => Game.Rayman3;
    public override GameCategory Category => GameCategory.Rayman;

    public override LocalizedString DisplayName => new ResourceLocString(nameof(Resources.Rayman3_Win32_Title));
    public override string[] SearchKeywords => new[] { "r3" };
    public override DateTime ReleaseDate => new(2003, 03, 18);

    public override GameIconAsset Icon => GameIconAsset.Rayman3;
    public override GameBannerAsset Banner => GameBannerAsset.Rayman3;

    #endregion

    #region Protected Methods

    protected override void RegisterComponents(IGameComponentBuilder builder)
    {
        base.RegisterComponents(builder);

        builder.Register(new UbisoftConnectGameClientComponent(UbisoftConnectGameId, UbisoftConnectProductId));

        builder.Register(new ProgressionManagersComponent(x => new GameProgressionManager_Rayman3_Win32(x, "Rayman 3")));
        builder.Register(new GameSettingsComponent(x => new Rayman3SettingsViewModel(x)));
        builder.Register<OnGameAddedComponent, AddToJumpListOnGameAddedComponent>();
        builder.Register<LocalGameLinksComponent>(new Rayman3SetupLocalGameLinksComponent(false));
        builder.Register(new PCGamingWikiComponent("Rayman_3:_Hoodlum_Havoc"));
        builder.Register(new RayMapComponent(RayMapComponent.RayMapViewer.RayMap, "r3_pc", "r3_pc"));
        builder.Register<BinaryGameModeComponent>(new CPAGameModeComponent(CPAGameMode.Rayman3_PC));
        builder.Register<ArchiveComponent>(new CPAArchiveComponent(_ => new[]
        {
            @"Gamedatabin\tex32_1.cnt",
            @"Gamedatabin\tex32_2.cnt",
            @"Gamedatabin\vignette.cnt",
        }));
        builder.Register(new CPATextureSyncComponent(
            new CPATextureSyncDataItem(
                Name: "Gamedatabin",
                Archives: new[] { "tex32_1.cnt", "tex32_2.cnt", "vignette.cnt" })));

        builder.Register(new GameBananaGameComponent(6188));
        builder.Register(new FilesModModuleExamplePaths(x => Path.GetFileName(x) switch
        {
            "tex32_1.cnt" => "charact",
            "tex32_2.cnt" => "charact",
            "" => "Gamedatabin",
            _ => null,
        }));

        builder.Register(new DiscordRichPresenceComponent("Rayman 3", "rayman_3"));
        builder.Register(new RichPresenceManagerComponent((g, p) => new GameRichPresenceManager_Rayman3_Win32(g, p)));
        builder.Register(new RuntimeModificationsGameManagersComponent(EmulatedPlatform.None, _ =>
            new[]
            {
                new CpaGameManager(
                    displayName: new ResourceLocString(nameof(Resources.Mod_Mem_Game_R3_PC)),
                    getOffsetsFunc: () => CPAMemoryData.Offsets_R3_PC)
            }));

        builder.Register(new SetupGameActionComponent(_ => new BetterRayman3SetupGameAction()));
        builder.Register(new SetupGameActionComponent(_ => new Rayman3PS2VideosSetupGameAction()));
        builder.Register(new SetupGameActionComponent(_ => new LevelTransparencyFixesSetupGameAction()));
    }

    protected override ProgramInstallationStructure CreateStructure() => new DirectoryProgramInstallationStructure(new ProgramFileSystem(new ProgramPath[]
    {
        // Files
        new ProgramFilePath("Rayman3.exe", ProgramPathType.PrimaryExe, required: true),

        // Directories
        new ProgramDirectoryPath("Gamedatabin", ProgramPathType.Data, required: true),
        new ProgramDirectoryPath("GAMEDATA", ProgramPathType.Save),
    }));

    #endregion

    #region Public Methods

    public override IEnumerable<GameAddAction> GetAddActions() => new GameAddAction[]
    {
        new LocateDirectoryGameAddAction(this),
    };

    public override IEnumerable<GamePurchaseLink> GetPurchaseLinks() => new GamePurchaseLink[]
    {
        new(new ResourceLocString(nameof(Resources.GameDisplay_PurchaseGOG)), "https://www.gog.com/game/rayman_3_hoodlum_havoc"),
        new(new ResourceLocString(nameof(Resources.GameDisplay_PurchaseUplay)), UbisoftConnectHelpers.GetStorePageURL(UbisoftConnectProductId)),
    };

    public override FinderQuery[] GetFinderQueries() => new FinderQuery[]
    {
        new UbiIniFinderQuery(Rayman3IniAppData.SectionName),

        new UninstallProgramFinderQuery("Rayman 3"),
        new UninstallProgramFinderQuery("Rayman: 3"),
        new UninstallProgramFinderQuery("Rayman 3 - Hoodlum Havoc"),
        new UninstallProgramFinderQuery("Rayman: 3 - Hoodlum Havoc"),
        new UninstallProgramFinderQuery("Rayman 3 Hoodlum Havoc"), // Ubisoft Connect

        new Win32ShortcutFinderQuery("Rayman 3"),

        new UbisoftConnectFinderQuery(UbisoftConnectGameId),
    };

    #endregion
}