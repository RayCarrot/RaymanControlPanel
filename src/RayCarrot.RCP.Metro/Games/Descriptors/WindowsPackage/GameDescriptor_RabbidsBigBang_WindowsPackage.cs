using RayCarrot.RCP.Metro.Games.Components;
using RayCarrot.RCP.Metro.Games.Structure;

namespace RayCarrot.RCP.Metro;

/// <summary>
/// The Rabbids Big Bang (Windows Package) game descriptor
/// </summary>
public sealed class GameDescriptor_RabbidsBigBang_WindowsPackage : WindowsPackageGameDescriptor
{
    #region Private Constant Fields

    private const string MicrosoftStoreID = "9WZDNCRFJCS3";

    #endregion

    #region Public Properties

    public override string GameId => "RabbidsBigBang_WindowsPackage";
    public override string LegacyGameId => "RabbidsBigBang";
    public override Game Game => Game.RabbidsBigBang;
    public override GameCategory Category => GameCategory.Rabbids;

    public override LocalizedString DisplayName => new ResourceLocString(nameof(Resources.RabbidsBigBang_WindowsPackage_Title));
    public override DateTime ReleaseDate => new(2014, 03, 05);

    public override GameIconAsset Icon => GameIconAsset.RabbidsBigBang;
    public override GameBannerAsset Banner => GameBannerAsset.RabbidsBigBang;

    public override string PackageName => "UbisoftEntertainment.RabbidsBigBang";
    public override string FullPackageName => "UbisoftEntertainment.RabbidsBigBang_dbgk1hhpxymar";

    #endregion

    #region Protected Methods

    protected override void RegisterComponents(IGameComponentBuilder builder)
    {
        base.RegisterComponents(builder);

        builder.Register(new ProgressionManagersComponent(x => new GameProgressionManager_RabbidsBigBang_WindowsPackage(this, x, "Rabbids Big Bang")));
        builder.Register<OnGameAddedComponent, AddToJumpListOnGameAddedComponent>();
        builder.Register<ExternalGameLinksComponent>(new MicrosoftStoreExternalGameLinksComponent(MicrosoftStoreID));
        builder.Register(new PCGamingWikiComponent("Rabbids_Big_Bang"));

        builder.Register(new DiscordRichPresenceComponent("Rabbids Big Bang", "rabbids_big_bang"));
    }

    protected override ProgramInstallationStructure CreateStructure() => new DirectoryProgramInstallationStructure(new ProgramFileSystem(new ProgramPath[]
    {
        // Files
        new ProgramFilePath("Template.exe", ProgramPathType.PrimaryExe, required: true),
    }));

    #endregion
}