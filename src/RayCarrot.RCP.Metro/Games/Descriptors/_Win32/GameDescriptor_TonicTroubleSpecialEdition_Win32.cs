using RayCarrot.RCP.Metro.Archive.CPA;
using RayCarrot.RCP.Metro.Games.Components;
using RayCarrot.RCP.Metro.Games.Finder;
using RayCarrot.RCP.Metro.Games.Structure;

namespace RayCarrot.RCP.Metro;

/// <summary>
/// The Tonic Trouble Special Edition (Win32) game descriptor
/// </summary>
public sealed class GameDescriptor_TonicTroubleSpecialEdition_Win32 : Win32GameDescriptor
{
    #region Public Properties

    public override string GameId => "TonicTroubleSpecialEdition_Win32";
    public override string LegacyGameId => "TonicTroubleSpecialEdition";
    public override Game Game => Game.TonicTrouble;
    public override GameCategory Category => GameCategory.Other;

    public override LocalizedString DisplayName => new ResourceLocString(nameof(Resources.TonicTroubleSpecialEdition_Win32_Title));
    public override string[] SearchKeywords => new[] { "ttse" };
    public override DateTime ReleaseDate => new(1998, 01, 01); // Not exact

    public override GameIconAsset Icon => GameIconAsset.TonicTroubleSpecialEdition;
    public override GameBannerAsset Banner => GameBannerAsset.TonicTrouble;

    #endregion

    #region Private Methods

    private static string GetLaunchArgs(GameInstallation gameInstallation) => "-cdrom:";

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

        builder.Register(new ProgressionManagersComponent(x => new GameProgressionManager_TonicTroubleSpecialEdition_Win32(x, "Tonic Trouble Special Edition")));
        builder.Register<OnGameAddedComponent, AddToJumpListOnGameAddedComponent>();
        builder.Register(new LaunchArgumentsComponent(GetLaunchArgs));
        builder.Register(new LocalGameLinksComponent(GetLocalGameLinks));
        builder.Register(new PCGamingWikiComponent("Tonic_Trouble_Special_Edition"));
        builder.Register(new RayMapComponent(RayMapComponent.RayMapViewer.RayMap, "ttse_pc", "ttse_pc"));
        builder.Register<BinaryGameModeComponent>(new CPAGameModeComponent(CPAGameMode.TonicTrouble_SE_PC));
        builder.Register<ArchiveComponent>(new CPAArchiveComponent(_ => new[]
        {
            @"GameData\Textures.cnt",
            @"GameData\Vignette.cnt",
        }));
        builder.Register(new CPATextureSyncComponent(
            new CPATextureSyncDataItem(
                Name: "GameData",
                Archives: new[] { "Textures.cnt", "Vignette.cnt" })));

        builder.Register(new GameBananaGameComponent(18937));

        builder.Register(new DiscordRichPresenceComponent("Tonic Trouble Special Edition", "tonic_trouble_special_edition"));
    }

    protected override ProgramInstallationStructure CreateStructure() => new DirectoryProgramInstallationStructure(new ProgramFileSystem(new ProgramPath[]
    {
        // Files
        new ProgramFilePath("MaiD3Dvr.exe", ProgramPathType.PrimaryExe, required: true),
    }));

    #endregion

    #region Public Methods

    public override IEnumerable<GameAddAction> GetAddActions() => new GameAddAction[]
    {
        new LocateDirectoryGameAddAction(this),
    };

    public override FinderQuery[] GetFinderQueries() => new FinderQuery[]
    {
        new UbiIniFinderQuery("TONICT"),

        new UninstallProgramFinderQuery("Tonic Trouble"),

        new Win32ShortcutFinderQuery("Tonic Trouble"),
    };

    #endregion
}