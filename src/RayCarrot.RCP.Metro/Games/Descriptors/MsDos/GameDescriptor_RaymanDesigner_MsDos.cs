using RayCarrot.RCP.Metro.Games.Components;
using RayCarrot.RCP.Metro.Games.Finder;
using RayCarrot.RCP.Metro.Games.Options;
using RayCarrot.RCP.Metro.Games.Settings;
using RayCarrot.RCP.Metro.Games.SetupGame;
using RayCarrot.RCP.Metro.Games.Structure;

namespace RayCarrot.RCP.Metro;

/// <summary>
/// The Rayman Designer (MS-DOS) game descriptor
/// </summary>
public sealed class GameDescriptor_RaymanDesigner_MsDos : MsDosGameDescriptor
{
    #region Private Constant Fields

    // NOTE: Same id as all Rayman Forever games
    private const string UbisoftConnectGameId = "2968";
    private const string UbisoftConnectProductId = "5800d3fc4e016524248b4567";

    #endregion

    #region Public Properties

    public override string GameId => "RaymanDesigner_MsDos";
    public override string LegacyGameId => "RaymanDesigner";
    public override Game Game => Game.RaymanDesigner;
    public override GameCategory Category => GameCategory.Rayman;
    
    public override LocalizedString DisplayName => new ResourceLocString(nameof(Resources.RaymanDesigner_MsDos_Title));
    public override string[] SearchKeywords => new[] { "rd" };
    public override DateTime ReleaseDate => new(1997, 01, 01); // Not exact

    public override GameIconAsset Icon => GameIconAsset.RaymanDesigner;
    public override GameBannerAsset Banner => GameBannerAsset.Rayman1;

    #endregion

    #region Private Methods

    private static IEnumerable<GameLinksComponent.GameUriLink> GetLocalGameLinks(GameInstallation gameInstallation) => new[]
    {
        new GameLinksComponent.GameUriLink(
            Header: new ResourceLocString(nameof(Resources.GameLink_RDMapper)), 
            Uri: gameInstallation.InstallLocation.Directory + "MAPPER.EXE")
    };

    #endregion

    #region Protected Methods

    protected override void RegisterComponents(IGameComponentBuilder builder)
    {
        base.RegisterComponents(builder);

        builder.Register(new UbisoftConnectGameClientComponent(UbisoftConnectGameId, UbisoftConnectProductId));

        builder.Register(new ProgressionManagersComponent(x => new GameProgressionManager_RaymanDesigner_MsDos(x, "Rayman Designer")));
        builder.Register<GameValidationCheckComponent, Ray1MsDosGameDataGameValidationCheckComponent>();
        builder.Register(new GameSettingsComponent(x => new Ray1SettingsViewModel(x)));
        builder.Register<OnGameAddedComponent, SetRay1MsDosDataOnGameAddedComponent>(ComponentPriority.High);
        builder.Register<OnGameAddedComponent, AddToJumpListOnGameAddedComponent>();
        builder.Register<OnGameAddedComponent, FindRaymanForeverFilesOnGameAddedComponent>();
        builder.Register<LaunchArgumentsComponent, Ray1LaunchArgumentsComponent>();
        builder.Register<MsDosGameRequiresDiscComponent>();
        builder.Register(new GameOptionsComponent(x => new Ray1MsDosGameOptionsViewModel(x)));
        builder.Register(new LocalGameLinksComponent(GetLocalGameLinks));
        builder.Register(new PCGamingWikiComponent("Rayman_Designer"));
        builder.Register(new RayMapComponent(RayMapComponent.RayMapViewer.Ray1Map, "RaymanDesignerPC", "r1/pc_kit"));
        builder.Register<BinaryGameModeComponent>(new Ray1GameModeComponent(Ray1GameMode.RaymanDesigner_PC));
        builder.Register(new Ray1ConfigFileNameComponent(_ => "RAYKIT.CFG"));
        builder.Register<ArchiveComponent, Ray1MsDosArchiveComponent>();

        builder.Register(new SetupGameActionComponent(_ => new RaymanForeverCompleteSoundtrackSetupGameAction()));
        builder.Register(new SetupGameActionComponent(_ => new ReplaceInfectedRaymanDesignerFilesSetupGameAction()));
        builder.Register(new SetupGameActionComponent(_ => new Ray1InvalidGameConfigSetupGameAction()));
        builder.Register(new SetupGameActionComponent(_ => new MissingMapperConfigSetupGameAction()));
    }

    protected override ProgramInstallationStructure CreateStructure() => new DirectoryProgramInstallationStructure(new ProgramFileSystem(new ProgramPath[]
    {
        // Files
        new ProgramFilePath("RAYKIT.EXE", ProgramPathType.PrimaryExe, required: true),

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
            const string gameName = "RayKit";

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