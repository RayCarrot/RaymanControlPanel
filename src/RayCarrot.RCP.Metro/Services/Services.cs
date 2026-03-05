using System.Net.Http;
using Microsoft.Extensions.DependencyInjection;
using RayCarrot.RCP.Metro.Games.Clients;
using RayCarrot.RCP.Metro.Games.Tools;

namespace RayCarrot.RCP.Metro;

// TODO: Clean up DI implementation in app:
//       - Get rid of all references to this static class. In most cases we want to get the services from the ctor.
//       - Refactor interfaces, maybe get rid of them until they're needed? Their abstraction isn't always helpful.
//       - Get rid of BaseRCPViewModel and its service caching. Better to have view models be transients in the DI container.

/// <summary>
/// Shortcuts for the common application services
/// </summary>
public static class Services
{
    private static IServiceProvider ServiceProvider => Metro.App.Current.ServiceProvider;
    private static T GetService<T>() where T : notnull => ServiceProvider.GetRequiredService<T>();

    /// <summary>
    /// Gets the common app data
    /// </summary>
    public static IAppInstanceData InstanceData => GetService<IAppInstanceData>();

    /// <summary>
    /// Gets the message UIManager
    /// </summary>
    public static IMessageUIManager MessageUI => GetService<IMessageUIManager>();

    /// <summary>
    /// Gets the browse UIManager
    /// </summary>
    public static IBrowseUIManager BrowseUI => GetService<IBrowseUIManager>();

    /// <summary>
    /// Gets the dialog base manager
    /// </summary>
    public static IDialogBaseManager DialogBaseManager => GetService<IDialogBaseManager>();

    public static AssociatedFileEditorsManager AssociatedFileEditorsManager => GetService<AssociatedFileEditorsManager>();

    /// <summary>
    /// The application user data
    /// </summary>
    public static AppUserData Data => GetService<AppUserData>();

    /// <summary>
    /// The app view model
    /// </summary>
    public static AppViewModel App => GetService<AppViewModel>();

    /// <summary>
    /// The App UI manager
    /// </summary>
    public static AppUIManager UI => GetService<AppUIManager>();

    /// <summary>
    /// The backup manager
    /// </summary>
    public static GameBackups_Manager Backup => GetService<GameBackups_Manager>();

    /// <summary>
    /// The file manager
    /// </summary>
    public static FileManager File => GetService<FileManager>();

    /// <summary>
    /// The games manager
    /// </summary>
    public static GamesManager Games => GetService<GamesManager>();

    /// <summary>
    /// The game clients manager
    /// </summary>
    public static GameClientsManager GameClients => GetService<GameClientsManager>();

    public static IMessenger Messenger => GetService<IMessenger>();

    public static JumpListManager JumpList => GetService<JumpListManager>();

    public static IHttpClientFactory HttpClientFactory => GetService<IHttpClientFactory>();

    public static RunningGamesManager RunningGamesManager => GetService<RunningGamesManager>();
}