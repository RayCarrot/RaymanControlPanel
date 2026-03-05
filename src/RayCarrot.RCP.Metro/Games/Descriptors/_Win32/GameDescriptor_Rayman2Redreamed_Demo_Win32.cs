using RayCarrot.RCP.Metro.Games.Components;
using RayCarrot.RCP.Metro.Games.Options;
using RayCarrot.RCP.Metro.Games.Structure;

namespace RayCarrot.RCP.Metro;

/// <summary>
/// The Rayman 2: Redreamed Demo (Win32) game descriptor
/// </summary>
public sealed class GameDescriptor_Rayman2Redreamed_Demo_Win32 : Win32GameDescriptor
{
    #region Constant Fields

    private const string GameJoltUrl = "https://gamejolt.com/games/rayman-2-redreamed/1004947";

    #endregion

    #region Public Properties

    public override string GameId => "Rayman2Redreamed_Demo_Win32";
    public override Game Game => Game.Rayman2Redreamed;
    public override GameCategory Category => GameCategory.Fan;
    public override GameType Type => GameType.Demo;

    public override LocalizedString DisplayName => new ResourceLocString(nameof(Resources.Rayman2Redreamed_Demo_Win32_Title));
    public override DateTime ReleaseDate => new(2025, 09, 01);

    public override GameIconAsset Icon => GameIconAsset.Rayman2Redreamed;
    public override GameBannerAsset Banner => GameBannerAsset.Rayman2Redreamed;

    #endregion

    #region Private Methods

    private static string? GetLaunchArgs(GameInstallation gameInstallation)
    {
        string? api = gameInstallation.GetValue<string>(GameDataKey.R2R_GraphicsApi);

        if (api == null)
            return null;
        else
            return $"-{api}";
    }

    #endregion

    #region Protected Methods

    protected override void RegisterComponents(IGameComponentBuilder builder)
    {
        base.RegisterComponents(builder);

        builder.Register(new GameOptionsComponent(x => new Rayman2RedreamedGameOptionsViewModel(x)));
        builder.Register(new LaunchArgumentsComponent(GetLaunchArgs));
        builder.Register<ExternalGameLinksComponent>(new GameJoltExternalGameLinksComponent(GameJoltUrl));

        builder.Register(new DiscordRichPresenceComponent("Rayman 2: Redreamed Demo", "rayman_2_redreamed"));
    }
    
    protected override ProgramInstallationStructure CreateStructure() => new DirectoryProgramInstallationStructure(new ProgramFileSystem(new ProgramPath[]
    {
        // Files
        new ProgramFilePath("R2R.exe", ProgramPathType.PrimaryExe, required: true),
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