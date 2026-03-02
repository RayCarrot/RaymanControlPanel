using RayCarrot.RCP.Metro.Games.Components;
using RayCarrot.RCP.Metro.Games.Settings;
using RayCarrot.RCP.Metro.Games.SetupGame;
using RayCarrot.RCP.Metro.Games.Structure;

namespace RayCarrot.RCP.Metro;

/// <summary>
/// The Rayman Fiesta Run (Windows Package) game descriptor
/// </summary>
public sealed class GameDescriptor_RaymanFiestaRun_WindowsPackage : WindowsPackageGameDescriptor
{
    #region Private Constant Fields

    private const string MicrosoftStoreID = "9wzdncrdds0c";

    #endregion

    #region Public Properties

    public override string GameId => "RaymanFiestaRun_WindowsPackage";
    public override string LegacyGameId => "RaymanFiestaRun";
    public override Game Game => Game.RaymanFiestaRun;
    public override GameCategory Category => GameCategory.Rayman;

    public override LocalizedString DisplayName => new ResourceLocString(nameof(Resources.RaymanFiestaRun_WindowsPackage_Title));
    public override string[] SearchKeywords => new[] { "rfr" };
    public override DateTime ReleaseDate => new(2014, 02, 12);

    public override GameIconAsset Icon => GameIconAsset.RaymanFiestaRun;
    public override GameBannerAsset Banner => GameBannerAsset.RaymanFiestaRun;

    public override string PackageName => "Ubisoft.RaymanFiestaRun";
    public override string FullPackageName => "Ubisoft.RaymanFiestaRun_ngz4m417e0mpw";

    #endregion

    #region Protected Methods

    protected override void RegisterComponents(IGameComponentBuilder builder)
    {
        base.RegisterComponents(builder);

        builder.Register(new ProgressionManagersComponent(x => new GameProgressionManager_RaymanFiestaRun_WindowsPackage(this, x, 
            // NOTE: This id is also defined in the app data migration code
            "Rayman Fiesta Run (Default)", 1)));
        builder.Register(new GameSettingsComponent(x => new RaymanFiestaRunSettingsViewModel(x, GetLocalAppDataDirectory(), false)));
        builder.Register<OnGameAddedComponent, AddToJumpListOnGameAddedComponent>();
        builder.Register<ExternalGameLinksComponent>(new MicrosoftStoreExternalGameLinksComponent(MicrosoftStoreID));
        builder.Register<BinaryGameModeComponent>(new UbiArtGameModeComponent(UbiArtGameMode.RaymanFiestaRun_PC));

        builder.Register(new PCGamingWikiComponent("Rayman_Fiesta_Run"));

        builder.Register(new SetupGameActionComponent(_ => new CorruptRaymanFiestaRunSaveFileSetupGameAction(this, 1)));

        builder.Register(new GameBananaGameComponent(19729));
    }

    protected override ProgramInstallationStructure CreateStructure() => new DirectoryProgramInstallationStructure(new ProgramFileSystem(new ProgramPath[]
    {
        // Files
        new ProgramFilePath("RFR_WinRT.exe", ProgramPathType.PrimaryExe, required: true),
    }));

    #endregion
}