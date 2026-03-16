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
/// The Rayman 2 (Win32) game descriptor
/// </summary>
public sealed class GameDescriptor_Rayman2_Win32 : Win32GameDescriptor
{
    #region Private Constant Fields

    private const string SteamId = "15060";

    private const string UbisoftConnectGameId = "361";
    private const string UbisoftConnectProductId = "56c4947e88a7e300458b465c";

    #endregion

    #region Public Properties

    public override string GameId => "Rayman2_Win32";
    public override string LegacyGameId => "Rayman2";
    public override Game Game => Game.Rayman2;
    public override GameCategory Category => GameCategory.Rayman;

    public override LocalizedString DisplayName => new ResourceLocString(nameof(Resources.Rayman2_Win32_Title));
    public override string[] SearchKeywords => new[] { "r2" };
    public override DateTime ReleaseDate => new(1999, 11, 05);

    public override GameIconAsset Icon => GameIconAsset.Rayman2;
    public override GameBannerAsset Banner => GameBannerAsset.Rayman2;

    #endregion

    #region Private Methods

    private static FileSystemPath GetLaunchFilePath(GameInstallation gameInstallation)
    {
        // Use the Twofold dll loader exe if available
        FileSystemPath twofoldFilePath = gameInstallation.InstallLocation.Directory + "Ray2x.exe";
        if (twofoldFilePath.FileExists)
            return twofoldFilePath;
        else
            return gameInstallation.InstallLocation.Directory + "Rayman2.exe";
    }

    private static IEnumerable<GameLinksComponent.GameUriLink> GetLocalGameLinks(GameInstallation gameInstallation) => new[]
    {
        new GameLinksComponent.GameUriLink(
            Header: new ResourceLocString(nameof(Resources.GameLink_Setup)), 
            Uri: gameInstallation.InstallLocation.Directory + "GXSetup.exe"),
        new GameLinksComponent.GameUriLink(
            Header: new ResourceLocString(nameof(Resources.GameLink_DirectX)),
            Uri: gameInstallation.InstallLocation.Directory + "dxcfg.exe"),
        new GameLinksComponent.GameUriLink(
            Header: new ResourceLocString(nameof(Resources.GameLink_R2nGlide)), 
            Uri: gameInstallation.InstallLocation.Directory + "nglide_config.exe"),
        new GameLinksComponent.GameUriLink(
            Header: new ResourceLocString(nameof(Resources.GameLink_R2dgVoodoo)), 
            Uri: gameInstallation.InstallLocation.Directory + "dgVoodooCpl.exe"),
        new GameLinksComponent.GameUriLink(
            Header: new ResourceLocString(nameof(Resources.GameLink_R2Fix)), 
            Uri: gameInstallation.InstallLocation.Directory + "R2FixCfg.exe"),
    };

    #endregion

    #region Protected Methods

    protected override void RegisterComponents(IGameComponentBuilder builder)
    {
        base.RegisterComponents(builder);

        builder.Register(new SteamGameClientComponent(SteamId));
        builder.Register(new UbisoftConnectGameClientComponent(UbisoftConnectGameId, UbisoftConnectProductId));

        builder.Register(new Win32LaunchPathComponent(GetLaunchFilePath));

        builder.Register(new ProgressionManagersComponent(x => new GameProgressionManager_Rayman2_Win32(x, "Rayman 2")));
        builder.Register(new GameSettingsComponent(x => new Rayman2SettingsViewModel(x)));
        builder.Register<OnGameAddedComponent, AddToJumpListOnGameAddedComponent>();
        builder.Register(new LocalGameLinksComponent(GetLocalGameLinks));
        builder.Register(new PCGamingWikiComponent("Rayman_2:_The_Great_Escape"));
        builder.Register(new RayMapComponent(RayMapComponent.RayMapViewer.RayMap, "r2_pc", "r2_pc"));
        builder.Register<BinaryGameModeComponent>(new CPAGameModeComponent(CPAGameMode.Rayman2_PC));
        builder.Register<ArchiveComponent>(new CPAArchiveComponent(_ => new[]
        {
            @"Data\Textures.cnt",
            @"Data\Vignette.cnt",
        }));
        builder.Register(new CPATextureSyncComponent(
            new CPATextureSyncDataItem(
                Name: "Data",
                Archives: new[] { "Textures.cnt", "Vignette.cnt" })));

        builder.Register(new GameBananaGameComponent(6244));
        builder.Register(new FilesModModuleExamplePaths(x => Path.GetFileName(x) switch
        {
            "Textures.cnt" => "textures_personnages\\divers",
            "" => "Data\\World\\Levels",
            _ => null,
        }));

        builder.Register(new DiscordRichPresenceComponent("Rayman 2", "rayman_2"));
        builder.Register(new RichPresenceManagerComponent((g, p) => new GameRichPresenceManager_Rayman2_Win32(g, p)));
        builder.Register(new RuntimeModificationsGameManagersComponent(EmulatedPlatform.None, _ =>
            new[]
            {
                new CpaGameManager(
                    displayName: new ResourceLocString(nameof(Resources.Mod_Mem_Game_R2_PC)),
                    getOffsetsFunc: () => CPAMemoryData.Offsets_R2_PC)
            }));

        builder.Register(new SetupGameActionComponent(_ => new Ray2FixSetupGameAction()));
        builder.Register(new SetupGameActionComponent(_ => new HigherQualityOfficialRayman2TexturesSetupGameAction()));
    }

    protected override ProgramInstallationStructure CreateStructure() => new DirectoryProgramInstallationStructure(new ProgramFileSystem(new ProgramPath[]
    {
        // Files
        new ProgramFilePath("Rayman2.exe", ProgramPathType.PrimaryExe, required: true),
        new ProgramFilePath("GXSetup.exe", ProgramPathType.ConfigExe),

        // Directories
        new ProgramDirectoryPath("Data", ProgramPathType.Data, required: true)
        {
            new ProgramDirectoryPath("Options", ProgramPathType.Save),
            new ProgramDirectoryPath("World", ProgramPathType.Data, required: true),
            new ProgramDirectoryPath("SaveGame", ProgramPathType.Save)
        },
    }));

    #endregion

    #region Public Methods

    public override IEnumerable<GameAddAction> GetAddActions() => new GameAddAction[]
    {
        new LocateDirectoryGameAddAction(this),
        new DiscInstallGameAddAction(this, new GameInstallerInfo(
            discFilesListFileName: "Rayman2",
            gameLogo: GameLogoAsset.Rayman2,
            gifFileNames: new[] { "ASTRO.gif", "CASK.gif", "CHASE.gif", "GLOB.gif", "RODEO.gif", },
            installFolderName: "Rayman 2"))
    };

    public override IEnumerable<GamePurchaseLink> GetPurchaseLinks() => new GamePurchaseLink[]
    {
        new(new ResourceLocString(nameof(Resources.GameDisplay_PurchaseGOG)), "https://www.gog.com/game/rayman_2_the_great_escape"),
        new(new ResourceLocString(nameof(Resources.GameDisplay_PurchaseUplay)), UbisoftConnectHelpers.GetStorePageURL(UbisoftConnectProductId))
    };

    public override FinderQuery[] GetFinderQueries() => new FinderQuery[]
    {
        new UbiIniFinderQuery(Rayman2IniAppData.SectionName),

        new UninstallProgramFinderQuery("Rayman 2"),
        new UninstallProgramFinderQuery("Rayman: 2"),
        new UninstallProgramFinderQuery("Rayman 2 - The Great Escape"),
        new UninstallProgramFinderQuery("Rayman: 2 - The Great Escape"),
        new UninstallProgramFinderQuery("GOG.com Rayman 2"),
        new UninstallProgramFinderQuery("Rayman 2 The Great Escape"), // Ubisoft Connect

        new Win32ShortcutFinderQuery("Rayman 2"),

        new SteamFinderQuery(SteamId),

        new UbisoftConnectFinderQuery(UbisoftConnectGameId),
    };

    #endregion
}