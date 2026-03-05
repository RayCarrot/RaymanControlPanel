using RayCarrot.RCP.Metro.Games.Components;
using RayCarrot.RCP.Metro.Games.Structure;

namespace RayCarrot.RCP.Metro;

/// <summary>
/// The Rayman Mystery of Polokus (Win32) game descriptor
/// </summary>
public sealed class GameDescriptor_RaymanMysteryOfPolokus_Win32 : Win32GameDescriptor
{
    #region Constant Fields

    private const string GameJoltUrl = "https://gamejolt.com/games/RaymanMysteryOfPolokus/1016835";

    #endregion

    #region Public Properties

    public override string GameId => "RaymanMysteryOfPolokus_Win32";
    public override Game Game => Game.RaymanMysteryOfPolokus;
    public override GameCategory Category => GameCategory.Fan;

    public override LocalizedString DisplayName => new ResourceLocString(nameof(Resources.RaymanMysteryOfPolokus_Win32_Title));
    public override DateTime ReleaseDate => new(2025, 09, 10);

    public override GameIconAsset Icon => GameIconAsset.RaymanMysteryOfPolokus;
    public override GameBannerAsset Banner => GameBannerAsset.RaymanMysteryOfPolokus;

    #endregion

    #region Protected Methods

    protected override void RegisterComponents(IGameComponentBuilder builder)
    {
        base.RegisterComponents(builder);

        builder.Register<ExternalGameLinksComponent>(new GameJoltExternalGameLinksComponent(GameJoltUrl));

        builder.Register(new DiscordRichPresenceComponent("Rayman Mystery of Polokus", "rayman_mystery_of_polokus"));
    }
    
    protected override ProgramInstallationStructure CreateStructure() => new DirectoryProgramInstallationStructure(new ProgramFileSystem(new ProgramPath[]
    {
        // Files
        new ProgramFilePath("Rayman Mystery Of Polokus 1.6 The Boss Update.exe", ProgramPathType.PrimaryExe, required: true),
    }));

    #endregion

    #region Public Methods

    public override IEnumerable<GameAddAction> GetAddActions() => new GameAddAction[]
    {
        new LocateDirectoryGameAddAction(this),
    };

    public override IEnumerable<GamePurchaseLink> GetPurchaseLinks() => new GamePurchaseLink[]
    {
        new(new ResourceLocString(nameof(Resources.GameDisplay_GameJolt)), GameJoltUrl, GenericIconKind.GameAction_Web),
    };

    #endregion
}