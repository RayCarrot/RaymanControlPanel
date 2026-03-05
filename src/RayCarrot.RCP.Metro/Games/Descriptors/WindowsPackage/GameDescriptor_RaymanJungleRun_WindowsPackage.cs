using RayCarrot.RCP.Metro.Games.Components;
using RayCarrot.RCP.Metro.Games.Settings;
using RayCarrot.RCP.Metro.Games.Structure;

namespace RayCarrot.RCP.Metro;

/// <summary>
/// The Rayman Jungle Run (Windows Package) game descriptor
/// </summary>
public sealed class GameDescriptor_RaymanJungleRun_WindowsPackage : WindowsPackageGameDescriptor
{
    #region Private Constant Fields

    private const string MicrosoftStoreID = "9WZDNCRFJ13P";

    #endregion

    #region Public Properties

    public override string GameId => "RaymanJungleRun_WindowsPackage";
    public override string LegacyGameId => "RaymanJungleRun";
    public override Game Game => Game.RaymanJungleRun;
    public override GameCategory Category => GameCategory.Rayman;

    public override LocalizedString DisplayName => new ResourceLocString(nameof(Resources.RaymanJungleRun_WindowsPackage_Title));
    public override string[] SearchKeywords => new[] { "rjr" };
    public override DateTime ReleaseDate => new(2013, 03, 07);

    public override GameIconAsset Icon => GameIconAsset.RaymanJungleRun;
    public override GameBannerAsset Banner => GameBannerAsset.RaymanJungleRun;

    public override string PackageName => "UbisoftEntertainment.RaymanJungleRun";
    public override string FullPackageName => "UbisoftEntertainment.RaymanJungleRun_dbgk1hhpxymar";

    #endregion

    #region Protected Methods

    protected override void RegisterComponents(IGameComponentBuilder builder)
    {
        base.RegisterComponents(builder);

        builder.Register(new ProgressionManagersComponent(x => new GameProgressionManager_RaymanJungleRun_WindowsPackage(this, x, "Rayman Jungle Run")));
        builder.Register(new GameSettingsComponent(x => new RaymanJungleRunSettingsViewModel(x, GetLocalAppDataDirectory(), true, true, false)));
        builder.Register<OnGameAddedComponent, AddToJumpListOnGameAddedComponent>();
        builder.Register<ExternalGameLinksComponent>(new MicrosoftStoreExternalGameLinksComponent(MicrosoftStoreID));
        builder.Register<BinaryGameModeComponent>(new UbiArtGameModeComponent(UbiArtGameMode.RaymanJungleRun_PC));

        builder.Register(new PCGamingWikiComponent("Rayman_Jungle_Run"));

        builder.Register(new GameBananaGameComponent(19728));

        builder.Register(new DiscordRichPresenceComponent("Rayman Jungle Run", "rayman_jungle_run"));
    }

    protected override ProgramInstallationStructure CreateStructure() => new DirectoryProgramInstallationStructure(new ProgramFileSystem(new ProgramPath[]
    {
        // Files
        new ProgramFilePath("RO1Mobile.exe", ProgramPathType.PrimaryExe, required: true),
    }));

    #endregion
}