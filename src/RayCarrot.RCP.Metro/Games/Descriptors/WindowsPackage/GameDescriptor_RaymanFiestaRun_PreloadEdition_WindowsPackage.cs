using RayCarrot.RCP.Metro.Games.Components;
using RayCarrot.RCP.Metro.Games.Settings;
using RayCarrot.RCP.Metro.Games.SetupGame;
using RayCarrot.RCP.Metro.Games.Structure;

namespace RayCarrot.RCP.Metro;

/// <summary>
/// The Rayman Fiesta Run Preload Edition (Windows Package) game descriptor
/// </summary>
public sealed class GameDescriptor_RaymanFiestaRun_PreloadEdition_WindowsPackage : WindowsPackageGameDescriptor
{
    #region Private Constant Fields

    private const string MicrosoftStoreID = "9wzdncrdcw9b";

    #endregion

    #region Public Properties

    public override string GameId => "RaymanFiestaRunPreloadEdition_WindowsPackage";
    public override string LegacyGameId => "RaymanFiestaRun";
    public override Game Game => Game.RaymanFiestaRun;
    public override GameCategory Category => GameCategory.Rayman;

    public override LocalizedString DisplayName => new ResourceLocString(nameof(Resources.RaymanFiestaRunPreloadEdition_WindowsPackage_Title));
    public override string[] SearchKeywords => new[] { "rfr" };
    public override DateTime ReleaseDate => new(2014, 06, 04);

    public override GameIconAsset Icon => GameIconAsset.RaymanFiestaRun;
    public override GameBannerAsset Banner => GameBannerAsset.RaymanFiestaRun;

    public override string PackageName => "UbisoftEntertainment.RaymanFiestaRunPreloadEdition";
    public override string FullPackageName => "UbisoftEntertainment.RaymanFiestaRunPreloadEdition_dbgk1hhpxymar";

    #endregion

    #region Protected Methods

    protected override void RegisterComponents(IGameComponentBuilder builder)
    {
        base.RegisterComponents(builder);

        builder.Register(new ProgressionManagersComponent(x => new GameProgressionManager_RaymanFiestaRun_WindowsPackage(this, x, "Rayman Fiesta Run (Preload)", 1)));
        builder.Register(new GameSettingsComponent(x => new RaymanFiestaRunSettingsViewModel(x, GetLocalAppDataDirectory(), false)));
        builder.Register<OnGameAddedComponent, AddToJumpListOnGameAddedComponent>();
        builder.Register<ExternalGameLinksComponent>(new MicrosoftStoreExternalGameLinksComponent(MicrosoftStoreID));
        builder.Register<BinaryGameModeComponent>(new UbiArtGameModeComponent(UbiArtGameMode.RaymanFiestaRun_PC));
        
        builder.Register(new PCGamingWikiComponent("Rayman_Fiesta_Run"));

        builder.Register(new SetupGameActionComponent(_ => new CorruptRaymanFiestaRunSaveFileSetupGameAction(this, 1)));

        builder.Register(new GameBananaGameComponent(19729));

        builder.Register(new DiscordRichPresenceComponent("Rayman Fiesta Run", "rayman_fiesta_run"));
    }

    protected override ProgramInstallationStructure CreateStructure() => new DirectoryProgramInstallationStructure(new ProgramFileSystem(new ProgramPath[]
    {
        // Files
        new ProgramFilePath("RFR_WinRT_OEM.exe", ProgramPathType.PrimaryExe, required: true),
    }));

    #endregion
}