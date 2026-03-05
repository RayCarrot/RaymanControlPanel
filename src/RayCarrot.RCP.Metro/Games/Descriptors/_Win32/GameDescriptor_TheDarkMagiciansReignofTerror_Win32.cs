using RayCarrot.RCP.Metro.Games.Components;
using RayCarrot.RCP.Metro.Games.Structure;

namespace RayCarrot.RCP.Metro;

/// <summary>
/// The Rayman The Dark Magician's Reign of Terror (Win32) game descriptor
/// </summary>
public sealed class GameDescriptor_TheDarkMagiciansReignofTerror_Win32 : Win32GameDescriptor
{
    #region Constant Fields

    private const string GameJoltUrl = "https://gamejolt.com/games/Rayman_The_Dark_Magicians_Reign_of_terror/237701";

    #endregion

    #region Public Properties

    public override string GameId => "TheDarkMagiciansReignofTerror_Win32";
    public override string LegacyGameId => "TheDarkMagiciansReignofTerror";
    public override Game Game => Game.TheDarkMagiciansReignofTerror;
    public override GameCategory Category => GameCategory.Fan;

    public override LocalizedString DisplayName => new ResourceLocString(nameof(Resources.TheDarkMagiciansReignofTerror_Win32_Title));
    public override DateTime ReleaseDate => new(2015, 07, 13); // A bit unclear what the actual date is

    public override GameIconAsset Icon => GameIconAsset.TheDarkMagiciansReignofTerror;
    public override GameBannerAsset Banner => GameBannerAsset.RaymanTheDarkMagiciansReignofTerror;

    #endregion

    #region Protected Methods

    protected override void RegisterComponents(IGameComponentBuilder builder)
    {
        base.RegisterComponents(builder);

        builder.Register(new ProgressionManagersComponent(x => new GameProgressionManager_TheDarkMagiciansReignofTerror_Win32(x, "Rayman The Dark Magicians Reign of Terror")));
        builder.Register<ExternalGameLinksComponent>(new GameJoltExternalGameLinksComponent(GameJoltUrl));

        builder.Register(new DiscordRichPresenceComponent("Rayman The Dark Magician's Reign of Terror", "rayman_the_dark_magicians_reign_of_terror"));
    }

    protected override ProgramInstallationStructure CreateStructure() => new DirectoryProgramInstallationStructure(new ProgramFileSystem(new ProgramPath[]
    {
        // Files
        new ProgramFilePath("Rayman! Dark Magician's reign of terror!.exe", ProgramPathType.PrimaryExe, required: true),
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