using RayCarrot.RCP.Metro.Games.Components;
using RayCarrot.RCP.Metro.Games.Finder;
using RayCarrot.RCP.Metro.Games.Settings;
using RayCarrot.RCP.Metro.Games.SetupGame;
using RayCarrot.RCP.Metro.Games.Structure;
using RayCarrot.RCP.Metro.Games.Tools.RuntimeModifications;

namespace RayCarrot.RCP.Metro;

/// <summary>
/// The Rayman 1 (MS-DOS) game descriptor
/// </summary>
public sealed class GameDescriptor_Rayman1_MsDos : MsDosGameDescriptor
{
    #region Private Constant Fields

    // NOTE: Same id as all Rayman Forever games
    private const string UbisoftConnectGameId = "2968";
    private const string UbisoftConnectProductId = "5800d3fc4e016524248b4567";

    #endregion

    #region Public Properties

    public override string GameId => "Rayman1_MsDos";
    public override string LegacyGameId => "Rayman1";
    public override Game Game => Game.Rayman1;
    public override GameCategory Category => GameCategory.Rayman;

    public override LocalizedString DisplayName => new ResourceLocString(nameof(Resources.Rayman1_MsDos_Title));
    public override string[] SearchKeywords => new[] { "r1", "ray1" };
    public override DateTime ReleaseDate => new(1995, 09, 01);

    public override GameIconAsset Icon => GameIconAsset.Rayman1;
    public override GameBannerAsset Banner => GameBannerAsset.Rayman1;

    #endregion

    #region Protected Methods

    protected override void RegisterComponents(IGameComponentBuilder builder)
    {
        base.RegisterComponents(builder);

        builder.Register(new UbisoftConnectGameClientComponent(UbisoftConnectGameId, UbisoftConnectProductId));

        builder.Register(new ProgressionManagersComponent(x => new GameProgressionManager_Rayman1_MsDos(x, "Rayman 1")));
        builder.Register(new GameSettingsComponent(x => new Ray1SettingsViewModel(x)));
        builder.Register<OnGameAddedComponent, AddToJumpListOnGameAddedComponent>();
        builder.Register<OnGameAddedComponent, FindRaymanForeverFilesOnGameAddedComponent>();
        builder.Register<MsDosGameRequiresDiscComponent>();
        builder.Register(new PCGamingWikiComponent("Rayman"));
        builder.Register(new RayMapComponent(RayMapComponent.RayMapViewer.Ray1Map, "RaymanPC_1_21", "r1/pc_121"));
        builder.Register<BinaryGameModeComponent>(new Ray1GameModeComponent(Ray1GameMode.Rayman1_PC));
        builder.Register(new Ray1ConfigFileNameComponent(_ => "RAYMAN.CFG"));

        builder.Register(new RuntimeModificationsGameManagersComponent(EmulatedPlatform.MsDos, _ => 
            new[]
            {
                new Ray1GameManager(
                    displayName: new ResourceLocString(nameof(Resources.Mod_Mem_Game_R1_PC_1_21)),
                    getOffsetsFunc: () => Ray1MemoryData.Offsets_PC_1_21)
            }));
        builder.Register<PerLevelSoundtrackComponent>();

        builder.Register(new SetupGameActionComponent(_ => new RaymanForeverCompleteSoundtrackSetupGameAction()));
        builder.Register(new SetupGameActionComponent(_ => new Ray1InvalidGameConfigSetupGameAction()));

        builder.Register(new GameBananaGameComponent(21305));
    }

    protected override ProgramInstallationStructure CreateStructure() => new DirectoryProgramInstallationStructure(new ProgramFileSystem(new ProgramPath[]
    {
        // Files
        new ProgramFilePath("RAYMAN.EXE", ProgramPathType.PrimaryExe, required: true),

        // Directories
        new ProgramDirectoryPath("PCMAP", ProgramPathType.Data, required: true),
    }));

    #endregion

    #region Public Methods

    public override IEnumerable<GameAddAction> GetAddActions() => new GameAddAction[]
    {
        new LocateDirectoryGameAddAction(this),
    };

    public override IEnumerable<GamePurchaseLink> GetPurchaseLinks() => new GamePurchaseLink[]
    {
        new(new ResourceLocString(nameof(Resources.GameDisplay_PurchaseGOG)), "https://www.gog.com/game/rayman_forever"),
        new(new ResourceLocString(nameof(Resources.GameDisplay_PurchaseUplay)), UbisoftConnectHelpers.GetStorePageURL(UbisoftConnectProductId))
    };

    public override FinderQuery[] GetFinderQueries()
    {
        static InstallLocation validateLocation(InstallLocation location)
        {
            const string gameName = "Rayman";

            if (location.Directory.Name.Equals("DOSBOX", StringComparison.OrdinalIgnoreCase))
                return new InstallLocation(location.Directory.Parent + gameName);
            else
                return new InstallLocation(location.Directory + gameName);
        }

        return new FinderQuery[]
        {
            new UninstallProgramFinderQuery("Rayman Forever") { ValidateLocationFunc = validateLocation },
            new Win32ShortcutFinderQuery("Rayman Forever") { ValidateLocationFunc = validateLocation },

            new UbisoftConnectFinderQuery(UbisoftConnectGameId) { ValidateLocationFunc = validateLocation },
        };
    }

    #endregion
}