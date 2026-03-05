using RayCarrot.RCP.Metro.Archive.CPA;
using RayCarrot.RCP.Metro.Games.Components;
using RayCarrot.RCP.Metro.Games.Finder;
using RayCarrot.RCP.Metro.Games.Structure;

namespace RayCarrot.RCP.Metro;

/// <summary>
/// The Donald Duck Quack Attack/Goin' Quackers (Win32) game descriptor
/// </summary>
public sealed class GameDescriptor_DonaldDuckQuackAttack_Win32 : Win32GameDescriptor
{
    #region Public Properties

    public override string GameId => "DonaldDuckQuackAttack_Win32";
    public override Game Game => Game.DonaldDuckQuackAttack;
    public override GameCategory Category => GameCategory.Other;

    public override LocalizedString DisplayName => new ResourceLocString(nameof(Resources.DonaldDuckQuackAttack_Win32_Title));
    public override string[] SearchKeywords => new[] { "dd" };
    public override DateTime ReleaseDate => new(2000, 12, 02);

    public override GameIconAsset Icon => GameIconAsset.DonaldDuckQuackAttack;
    public override GameBannerAsset Banner => GameBannerAsset.DonaldDuckQuackAttack;

    #endregion

    #region Private Methods

    private static IEnumerable<GameLinksComponent.GameUriLink> GetLocalGameLinks(GameInstallation gameInstallation) => new[]
    {
        new GameLinksComponent.GameUriLink(
            Header: new ResourceLocString(nameof(Resources.GameLink_R2dgVoodoo)), 
            Uri: gameInstallation.InstallLocation.Directory + "dgVoodooCpl.exe"),
    };

    #endregion

    #region Protected Methods

    protected override void RegisterComponents(IGameComponentBuilder builder)
    {
        base.RegisterComponents(builder);

        // TODO: Add progression support
        builder.Register<OnGameAddedComponent, AddToJumpListOnGameAddedComponent>();
        builder.Register(new LocalGameLinksComponent(GetLocalGameLinks));
        builder.Register(new PCGamingWikiComponent("Donald_Duck:_Goin'_Quackers"));
        builder.Register(new RayMapComponent(RayMapComponent.RayMapViewer.RayMap, "dd_pc", "dd_pc"));
        builder.Register<BinaryGameModeComponent>(new CPAGameModeComponent(CPAGameMode.DonaldDuck_PC));
        builder.Register<ArchiveComponent>(new CPAArchiveComponent(_ => new[]
        {
            @"Data\Textures.cnt",
            @"Data\Vignette.cnt",
        }));
        builder.Register(new CPATextureSyncComponent(
            new CPATextureSyncDataItem(
                Name: "Data",
                Archives: new[] { "Textures.cnt", "Vignette.cnt" })));

        builder.Register(new DiscordRichPresenceComponent("Donald Duck Quack Attack/Goin' Quackers", "donald_duck_quack_attack"));
    }

    protected override ProgramInstallationStructure CreateStructure() => new DirectoryProgramInstallationStructure(new ProgramFileSystem(new ProgramPath[]
    {
        // Files
        new ProgramFilePath("Donald.exe", ProgramPathType.PrimaryExe, required: true),
    }));

    #endregion

    #region Public Methods

    public override IEnumerable<GameAddAction> GetAddActions() => new GameAddAction[]
    {
        new LocateDirectoryGameAddAction(this),
    };

    public override FinderQuery[] GetFinderQueries() => new FinderQuery[]
    {
        new Win32ShortcutFinderQuery("Donald"),
    };

    #endregion
}