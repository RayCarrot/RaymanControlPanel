using RayCarrot.RCP.Metro.Games.Components;
using RayCarrot.RCP.Metro.Games.Settings;
using RayCarrot.RCP.Metro.Games.SetupGame;
using RayCarrot.RCP.Metro.Games.Structure;

namespace RayCarrot.RCP.Metro;

/// <summary>
/// The Rayman Fiesta Run Windows 10 Edition (Windows Package) game descriptor
/// </summary>
public sealed class GameDescriptor_RaymanFiestaRun_Windows10Edition_WindowsPackage : WindowsPackageGameDescriptor
{
    #region Private Constant Fields

    private const string MicrosoftStoreID = "9nblggh59m6b";

    #endregion

    #region Public Properties

    public override string GameId => "RaymanFiestaRunWindows10Edition_WindowsPackage";
    public override string LegacyGameId => "RaymanFiestaRun";
    public override Game Game => Game.RaymanFiestaRun;
    public override GameCategory Category => GameCategory.Rayman;

    public override LocalizedString DisplayName => new ResourceLocString(nameof(Resources.RaymanFiestaRunWindows10Edition_WindowsPackage_Title));
    public override string[] SearchKeywords => new[] { "rfr" };
    public override DateTime ReleaseDate => new(2016, 04, 05);

    public override GameIconAsset Icon => GameIconAsset.RaymanFiestaRun;
    public override GameBannerAsset Banner => GameBannerAsset.RaymanFiestaRun;

    public override string PackageName => "Ubisoft.RaymanFiestaRunWindows10Edition";
    public override string FullPackageName => "Ubisoft.RaymanFiestaRunWindows10Edition_ngz4m417e0mpw";

    #endregion

    #region Protected Methods

    protected override void RegisterComponents(IGameComponentBuilder builder)
    {
        base.RegisterComponents(builder);

        builder.Register(new ProgressionManagersComponent(x => new GameProgressionManager_RaymanFiestaRun_WindowsPackage(this, x,
            // NOTE: This id is also defined in the app data migration code
            "Rayman Fiesta Run (Win10)", 0)));
        builder.Register(new GameSettingsComponent(x => new RaymanFiestaRunSettingsViewModel(x, GetLocalAppDataDirectory(), false)));
        builder.Register<OnGameAddedComponent, AddToJumpListOnGameAddedComponent>();
        builder.Register<ExternalGameLinksComponent>(new MicrosoftStoreExternalGameLinksComponent(MicrosoftStoreID));
        builder.Register<BinaryGameModeComponent>(new UbiArtGameModeComponent(UbiArtGameMode.RaymanFiestaRun_PC));

        builder.Register(new PCGamingWikiComponent("Rayman_Fiesta_Run"));

        builder.Register(new SetupGameActionComponent(_ => new CorruptRaymanFiestaRunSaveFileSetupGameAction(this, 0)));

        builder.Register(new GameBananaGameComponent(19729));
    }

    protected override ProgramInstallationStructure CreateStructure() => new DirectoryProgramInstallationStructure(new ProgramFileSystem(new ProgramPath[]
    {
        // Files
        new ProgramFilePath("RFRXAML.exe", ProgramPathType.PrimaryExe, required: true),
    }));

    #endregion
}