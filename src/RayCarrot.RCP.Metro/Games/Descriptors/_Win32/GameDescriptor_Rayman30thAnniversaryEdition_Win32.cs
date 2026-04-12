using System.IO;
using RayCarrot.RCP.Metro.Games.Components;
using RayCarrot.RCP.Metro.Games.Finder;
using RayCarrot.RCP.Metro.Games.Options;
using RayCarrot.RCP.Metro.Games.Settings;
using RayCarrot.RCP.Metro.Games.SetupGame;
using RayCarrot.RCP.Metro.Games.Structure;
using RayCarrot.RCP.Metro.ModLoader.Modules.BakesaleResource;
using RayCarrot.RCP.Metro.ModLoader.Modules.Rayman30thMsDosMusic;

namespace RayCarrot.RCP.Metro;

/// <summary>
/// The Rayman 30th Anniversary Edition (Win32) game descriptor
/// </summary>
public sealed class GameDescriptor_Rayman30thAnniversaryEdition_Win32 : Win32GameDescriptor
{
    #region Private Constant Fields

    private const string SteamId = "4094670";

    private const string UbisoftConnectGameId = "6220";
    private const string UbisoftConnectProductId = "69683b8797044c480eb79e04";

    #endregion

    #region Public Properties

    public override string GameId => "Rayman30thAnniversaryEdition_Win32";
    public override Game Game => Game.Rayman1;
    public override GameCategory Category => GameCategory.Rayman;

    public override LocalizedString DisplayName => new ResourceLocString(nameof(Resources.Rayman30thAnniversaryEdition_Win32_Title));
    public override string[] SearchKeywords => new[] { "r1", "ray1" };
    public override DateTime ReleaseDate => new(2026, 02, 13);

    public override GameIconAsset Icon => GameIconAsset.Rayman30thAnniversaryEdition;
    public override GameBannerAsset Banner => GameBannerAsset.Rayman30thAnniversaryEdition;

    #endregion

    #region Private Methods

    private static string? GetLaunchArgs(GameInstallation gameInstallation)
    {
        string? api = gameInstallation.GetValue<string>(GameDataKey.R30th_GraphicsApi);

        if (api == null)
            return null;
        else
            return $"--gfx {api}";
    }

    #endregion

    #region Protected Methods

    protected override void RegisterComponents(IGameComponentBuilder builder)
    {
        base.RegisterComponents(builder);

        builder.Register(new SteamGameClientComponent(SteamId, usesSteamStubDrm: true));
        builder.Register(new UbisoftConnectGameClientComponent(UbisoftConnectGameId, UbisoftConnectProductId));
        
        builder.Register(new ProgressionManagersComponent(x => 
        [
            new GameProgressionManager_Rayman30thAnniversaryEdition_RaymanPrototypeSnes_Win32(this, x, 
                "Rayman 30th Anniversary Edition", "snes_rayman_proto"),
            new GameProgressionManager_Rayman30thAnniversaryEdition_RaymanPs1_Win32(this, x, 
                "Rayman 30th Anniversary Edition", "ps1_rayman"),
            new GameProgressionManager_Rayman30thAnniversaryEdition_RaymanJaguar_Win32(this, x, 
                "Rayman 30th Anniversary Edition", "jaguar_rayman"),
            new GameProgressionManager_Rayman30thAnniversaryEdition_RaymanMsDos_Win32(this, x, 
                "Rayman 30th Anniversary Edition", "dreamm_rayman"),
            new GameProgressionManager_Rayman30thAnniversaryEdition_RaymansNewLevelsMsDos_Win32(this, x, 
                "Rayman 30th Anniversary Edition", "dreamm_rayman_new_levels"),
            new GameProgressionManager_Rayman30thAnniversaryEdition_RaymanByHisFansMsDos_Win32(this, x, 
                "Rayman 30th Anniversary Edition", "dreamm_rayman_fan_levels"),
            new GameProgressionManager_Rayman30thAnniversaryEdition_Rayman60LevelsMsDos_Win32(this, x, 
                "Rayman 30th Anniversary Edition", "dreamm_rayman_60_levels"),
            new GameProgressionManager_Rayman30thAnniversaryEdition_RaymanGbc_Win32(this, x, 
                "Rayman 30th Anniversary Edition", "gbc_rayman"),
            new GameProgressionManager_Rayman30thAnniversaryEdition_RaymanAdvanceGba_Win32(this, x, 
                "Rayman 30th Anniversary Edition", "gba_rayman"),
        ]));
        builder.Register(new RayMapComponent(RayMapComponent.RayMapViewer.Ray1Map, "RaymanPS1US", "r1/ps1_us"));
        builder.Register<BinaryGameModeComponent>(new BakesaleGameModeComponent(BakesaleGameMode.Rayman30thAnniversaryEdition_PC));
        builder.Register<ArchiveComponent, BakesaleArchiveComponent>();
        builder.Register(new GameOptionsComponent(x => new Rayman30thGameOptionsViewModel(x)));
        builder.Register(new LaunchArgumentsComponent(GetLaunchArgs));
        builder.Register(new GameSettingsComponent(x => new Rayman30thSettingsViewModel(this, x)));
        builder.Register<OnGameAddedComponent, AddToJumpListOnGameAddedComponent>();

        builder.Register(new PCGamingWikiComponent("Rayman:_30th_Anniversary_Edition"));

        builder.Register(new GameBananaGameComponent(24267));
        builder.Register(new FilesModModuleExamplePaths(x => Path.GetFileName(x) switch
        {
            "assets.pie" => "roms/gba",
            "" => "pancake",
            _ => null,
        }));
        builder.Register(new ModModuleComponent(_ => new Rayman30thMsDosMusicModule()));
        builder.Register(new ModModuleComponent(_ => new BakesaleResourceModule()));

        builder.Register(new SetupGameActionComponent(_ => new Rayman30thTechnicalDosFixesSetupGameAction()));
        builder.Register(new SetupGameActionComponent(_ => new Rayman30thAccessAllByHisFansLevelsSetupGameAction()));
        builder.Register(new SetupGameActionComponent(_ => new Rayman30thAccessUbiKeyBonusSetupGameAction()));

        builder.Register(new DiscordRichPresenceComponent("Rayman 30th Anniversary Edition", "rayman_30th_anniversary_edition"));
    }

    protected override ProgramInstallationStructure CreateStructure() => new DirectoryProgramInstallationStructure(new ProgramFileSystem(new ProgramPath[]
    {
        // Files
        new ProgramFilePath("rayman30th.exe", ProgramPathType.PrimaryExe, required: true),
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
        new UninstallProgramFinderQuery("Rayman: 30th Anniversary Edition"),

        new Win32ShortcutFinderQuery("Rayman 30th Anniversary Edition"),

        new SteamFinderQuery(SteamId),

        new UbisoftConnectFinderQuery(UbisoftConnectGameId),
    };

    public FileSystemPath GetSaveDirectory(GameInstallation gameInstallation)
    {
        FileSystemPath baseSavePath = Environment.SpecialFolder.ApplicationData.GetFolderPath() + "Rayman 30th Anniversary Edition";
        FileSystemPath savePath = UbisoftConnectHelpers.GetSaveDirectory(gameInstallation, baseSavePath);

        if (savePath != FileSystemPath.EmptyPath)
            return savePath;
        else
            return baseSavePath;
    }

    #endregion
}