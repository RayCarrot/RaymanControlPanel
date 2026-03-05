using RayCarrot.RCP.Metro.Games.Components;
using RayCarrot.RCP.Metro.Games.Finder;
using RayCarrot.RCP.Metro.Games.Structure;

namespace RayCarrot.RCP.Metro;

/// <summary>
/// The Rabbids Coding (Win32) game descriptor
/// </summary>
public sealed class GameDescriptor_RabbidsCoding_Win32 : Win32GameDescriptor
{
    #region Private Constant Fields

    private const string UbisoftConnectGameId = "5408";
    private const string UbisoftConnectProductId = "5d96f9b05cdf9a2eacdf68cb";

    #endregion

    #region Public Properties

    public override string GameId => "RabbidsCoding_Win32";
    public override string LegacyGameId => "RabbidsCoding";
    public override Game Game => Game.RabbidsCoding;
    public override GameCategory Category => GameCategory.Rabbids;

    public override LocalizedString DisplayName => new ResourceLocString(nameof(Resources.RabbidsCoding_Win32_Title));
    public override DateTime ReleaseDate => new(2019, 10, 08);

    public override GameIconAsset Icon => GameIconAsset.RabbidsCoding;

    #endregion

    #region Protected Methods

    protected override void RegisterComponents(IGameComponentBuilder builder)
    {
        base.RegisterComponents(builder);

        builder.Register(new UbisoftConnectGameClientComponent(UbisoftConnectGameId, UbisoftConnectProductId));
        builder.Register(new PCGamingWikiComponent("Rabbids_Coding!"));

        builder.Register(new DiscordRichPresenceComponent("Rabbids Coding", "rabbids_coding"));
    }

    protected override ProgramInstallationStructure CreateStructure() => new DirectoryProgramInstallationStructure(new ProgramFileSystem(new ProgramPath[]
    {
        // Files
        new ProgramFilePath("Rabbids Coding.exe", ProgramPathType.PrimaryExe, required: true),
    }));

    #endregion

    #region Public Methods

    public override IEnumerable<GameAddAction> GetAddActions() => new GameAddAction[]
    {
        new LocateDirectoryGameAddAction(this),
    };

    public override IEnumerable<GamePurchaseLink> GetPurchaseLinks() => new GamePurchaseLink[]
    {
        new(new ResourceLocString(nameof(Resources.GameDisplay_DownloadUplay)), UbisoftConnectHelpers.GetStorePageURL(UbisoftConnectProductId))
    };

    public override FinderQuery[] GetFinderQueries() => new FinderQuery[]
    {
        new UninstallProgramFinderQuery("RabbidsCoding"),
        new UninstallProgramFinderQuery("Rabbids Coding"),

        new Win32ShortcutFinderQuery("Rabbids Coding"),

        new UbisoftConnectFinderQuery(UbisoftConnectGameId),
    };

    #endregion
}