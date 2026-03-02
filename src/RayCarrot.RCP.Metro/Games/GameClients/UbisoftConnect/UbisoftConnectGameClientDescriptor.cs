using System.IO;
using RayCarrot.RCP.Metro.Games.Components;
using RayCarrot.RCP.Metro.Games.Finder;

namespace RayCarrot.RCP.Metro.Games.Clients.UbisoftConnect;

public class UbisoftConnectGameClientDescriptor : GameClientDescriptor
{
    #region Logger

    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    #endregion

    #region Public Properties

    public override string GameClientId => "UbisoftConnect";
    public override LocalizedString DisplayName => new ResourceLocString(nameof(Resources.GameClients_UbisoftConnect));
    public override GameClientIconAsset Icon => GameClientIconAsset.UbisoftConnect;

    #endregion

    #region Private Methods

    private static IEnumerable<DuoGridItemViewModel> GetGameInfoItems(GameInstallation gameInstallation) => new[]
    {
        new DuoGridItemViewModel(
            header: new ResourceLocString(nameof(Resources.GameInfo_UbisoftGameId)),
            text: gameInstallation.GetRequiredComponent<UbisoftConnectGameClientComponent>().GameId,
            minUserLevel: UserLevel.Advanced)
    };

    private static IEnumerable<GameLinksComponent.GameUriLink> GetExternalUriLinks(GameInstallation gameInstallation)
    {
        string productId = gameInstallation.GetRequiredComponent<UbisoftConnectGameClientComponent>().ProductId;

        return new[]
        {
            new GameLinksComponent.GameUriLink(
                Header: new ResourceLocString(nameof(Resources.GameDisplay_OpenInUbisoftStore)),
                Uri: UbisoftConnectHelpers.GetStorePageURL(productId),
                AssetIcon: WebIconAsset.UbisoftConnect)
        };
    }

    #endregion

    #region Public Methods

    public override void RegisterComponents(IGameComponentBuilder builder)
    {
        base.RegisterComponents(builder);

        builder.Register(new GameInfoComponent(GetGameInfoItems));
        builder.Register<LaunchGameComponent, UbisoftConnectLaunchGameComponent>();
        builder.Register(new ExternalGameLinksComponent(GetExternalUriLinks));
    }

    public override GameClientOptionsViewModel? GetGameClientOptionsViewModel(GameClientInstallation gameClientInstallation)
    {
        List<string> userIds = TryGetUserIds(gameClientInstallation).ToList();

        // Don't show options if there are no user ids to choose from
        if (userIds.Count == 0)
            return null;

        return new UbisoftConnectGameClientOptionsViewModel(gameClientInstallation, userIds);
    }

    public override bool SupportsGame(GameInstallation gameInstallation, GameClientInstallation gameClientInstallation) =>
        base.SupportsGame(gameInstallation, gameClientInstallation) && gameInstallation.HasComponent<UbisoftConnectGameClientComponent>();

    public override async Task OnGameClientAddedAsync(GameClientInstallation gameClientInstallation)
    {
        await base.OnGameClientAddedAsync(gameClientInstallation);

        // Set a default user id
        if (TryGetUserIds(gameClientInstallation).FirstOrDefault() is { } userId)
            gameClientInstallation.SetValue<string>(GameClientDataKey.UbisoftConnect_UserId, userId);
    }

    public override FinderQuery[] GetFinderQueries() => new FinderQuery[]
    {
        new UninstallProgramFinderQuery("Ubisoft Connect"),

        new Win32ShortcutFinderQuery("Ubisoft Connect"),
        new Win32ShortcutFinderQuery("Uplay"),
    };

    public IEnumerable<string> TryGetUserIds(GameClientInstallation gameClientInstallation)
    {
        // Try and find available ids by checking common locations

        try
        {
            FileSystemPath saveDir = gameClientInstallation.InstallLocation.Directory + "savegames";

            if (saveDir.DirectoryExists)
            {
                string[] subDirs = Directory.GetDirectories(saveDir);

                if (subDirs.Length > 0)
                    return subDirs.Select(x => new FileSystemPath(x).Name).Where(x => Guid.TryParse(x, out _));
            }
        }
        catch (Exception ex)
        {
            Logger.Warn(ex, "Getting available Ubisoft Connect user ids from savegames folder");
        }

        try
        {
            FileSystemPath cacheSettingsDir = gameClientInstallation.InstallLocation.Directory + "cache\\settings";

            if (cacheSettingsDir.DirectoryExists)
            {
                string[] files = Directory.GetFiles(cacheSettingsDir);

                if (files.Length > 0)
                    return files.Select(x => new FileSystemPath(x).Name).Where(x => Guid.TryParse(x, out _));
            }
        }
        catch (Exception ex)
        {
            Logger.Warn(ex, "Getting available Ubisoft Connect user ids from cache settings folder");
        }

        return Enumerable.Empty<string>();
    }

    #endregion
}