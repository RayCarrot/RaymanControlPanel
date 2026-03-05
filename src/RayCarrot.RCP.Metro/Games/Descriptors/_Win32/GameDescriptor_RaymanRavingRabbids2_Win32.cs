using RayCarrot.RCP.Metro.Games.Components;
using RayCarrot.RCP.Metro.Games.Data;
using RayCarrot.RCP.Metro.Games.Finder;
using RayCarrot.RCP.Metro.Games.Options;
using RayCarrot.RCP.Metro.Games.Settings;
using RayCarrot.RCP.Metro.Games.Structure;

namespace RayCarrot.RCP.Metro;

/// <summary>
/// The Rayman Raving Rabbids 2 (Win32) game descriptor
/// </summary>
public sealed class GameDescriptor_RaymanRavingRabbids2_Win32 : Win32GameDescriptor
{
    #region Public Overrides

    public override string GameId => "RaymanRavingRabbids2_Win32";
    public override string LegacyGameId => "RaymanRavingRabbids2";
    public override Game Game => Game.RaymanRavingRabbids2;
    public override GameCategory Category => GameCategory.Rabbids;

    public override LocalizedString DisplayName => new ResourceLocString(nameof(Resources.RaymanRavingRabbids2_Win32_Title));
    public override string[] SearchKeywords => new[] { "rrr2" };
    public override DateTime ReleaseDate => new(2008, 06, 05);

    public override GameIconAsset Icon => GameIconAsset.RaymanRavingRabbids2;
    public override GameBannerAsset Banner => GameBannerAsset.RaymanRavingRabbids;

    #endregion

    #region Private Methods

    private static string GetLaunchArgs(GameInstallation gameInstallation)
    {
        RaymanRavingRabbids2LaunchMode launchMode = gameInstallation.GetValue(GameDataKey.RRR2_LaunchMode, RaymanRavingRabbids2LaunchMode.AllGames);
        return $"/{launchMode.ToString().ToLower()} /B Rrr2.bf";
    }

    private static IEnumerable<GameLinksComponent.GameUriLink> GetLocalGameLinks(GameInstallation gameInstallation)
    {
        RaymanRavingRabbids2LaunchMode launchMode = gameInstallation.GetValue(GameDataKey.RRR2_LaunchMode, RaymanRavingRabbids2LaunchMode.AllGames);

        return new[]
        {
            new GameLinksComponent.GameUriLink(
                Header: new ResourceLocString(nameof(Resources.GameLink_Setup)), 
                Uri: gameInstallation.InstallLocation.Directory + "SettingsApplication.exe",
                Arguments: $"/{launchMode.ToString().ToLower()}")
        };
    }

    #endregion

    #region Protected Methods

    protected override void RegisterComponents(IGameComponentBuilder builder)
    {
        base.RegisterComponents(builder);

        builder.Register(new GameOptionsComponent(x => new RaymanRavingRabbids2GameOptionsViewModel(x)));
        builder.Register(new ProgressionManagersComponent(x => new GameProgressionManager_RaymanRavingRabbids2_Win32(x, "Rayman Raving Rabbids 2")));
        builder.Register(new GameSettingsComponent(x => new RaymanRavingRabbids2SettingsViewModel(x)));
        builder.Register<OnGameAddedComponent, AddToJumpListOnGameAddedComponent>();
        builder.Register(new LaunchArgumentsComponent(GetLaunchArgs));
        builder.Register(new LocalGameLinksComponent(GetLocalGameLinks));
        builder.Register<BinaryGameModeComponent>(new JadeGameModeComponent(JadeGameMode.RaymanRavingRabbids2_PC));

        builder.Register(new PCGamingWikiComponent("Rayman_Raving_Rabbids_2"));

        builder.Register(new DiscordRichPresenceComponent("Rayman Raving Rabbids 2", "rayman_raving_rabbids_2"));
    }

    protected override ProgramInstallationStructure CreateStructure() => new DirectoryProgramInstallationStructure(new ProgramFileSystem(new ProgramPath[]
    {
        // Files
        new ProgramFilePath("Jade.exe", ProgramPathType.PrimaryExe, required: true),
    }));

    #endregion

    #region Public Methods

    public override IEnumerable<GameAddAction> GetAddActions() => new GameAddAction[]
    {
        new LocateDirectoryGameAddAction(this),
    };

    public override FinderQuery[] GetFinderQueries() => new FinderQuery[]
    {
        new UninstallProgramFinderQuery("Rayman Raving Rabbids 2"),
        new UninstallProgramFinderQuery("Rayman: Raving Rabbids 2"),
        new UninstallProgramFinderQuery("Rayman Raving Rabbids 2 Orange"),
        new UninstallProgramFinderQuery("Rayman: Raving Rabbids 2 Orange"),
        new UninstallProgramFinderQuery("RRR2"),

        new Win32ShortcutFinderQuery("Rayman Raving Rabbids 2"),
    };

    #endregion
}