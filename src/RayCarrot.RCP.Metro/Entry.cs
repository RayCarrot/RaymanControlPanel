using Microsoft.Extensions.DependencyInjection;
using RayCarrot.RCP.Metro.Games.Clients;
using RayCarrot.RCP.Metro.Pages.About;
using RayCarrot.RCP.Metro.Pages.Debug;
using RayCarrot.RCP.Metro.Pages.Games;
using RayCarrot.RCP.Metro.Pages.Progression;
using RayCarrot.RCP.Metro.Pages.Settings;
using RayCarrot.RCP.Metro.Pages.Utilities;

namespace RayCarrot.RCP.Metro;

/// <summary>
/// The custom entry point for the application
/// </summary>
public static class Entry
{
    private static LaunchArguments _launchArguments = null!;

    [STAThread]
    public static void Main(string[] args)
    {
        // Save the launch arguments
        _launchArguments = new LaunchArguments(args);

        // Some tasks we have to run as admin. For these the app is restarted an admin worker mode.
        if (_launchArguments.HasArg(AdminWorker.MainArg))
        {
            AdminWorker.Run(_launchArguments);
            return;
        }

        // Set up the services
        IServiceCollection services = new ServiceCollection();
        ConfigureServices(services);
        IServiceProvider serviceProvider = services.BuildServiceProvider();

        // Create the Application, initialize it and start the message pump
        App app = new(serviceProvider);
        app.InitializeComponent();
        app.Run();
    }

    private static void ConfigureServices(IServiceCollection serviceCollection)
    {
        // Add services from other packages
        serviceCollection.AddHttpClient();

        // Add app related services
        serviceCollection.AddSingleton(_launchArguments);
        serviceCollection.AddSingleton<JumpListManager>();
        serviceCollection.AddSingleton<LoggerManager>();
        serviceCollection.AddSingleton<AppDataManager>();
        serviceCollection.AddTransient<StartupManager>();
        serviceCollection.AddTransient<LicenseManager>();
        serviceCollection.AddSingleton<GamesManager>();
        serviceCollection.AddSingleton<GameClientsManager>();
        serviceCollection.AddSingleton<AppViewModel>();
        serviceCollection.AddSingleton<AppUserData>();
        serviceCollection.AddSingleton<IAppInstanceData, AppInstanceData>();
        serviceCollection.AddSingleton<FileManager>();
        serviceCollection.AddSingleton<IUpdaterManager, UpdaterManager>();
        serviceCollection.AddSingleton<GameBackups_Manager>();
        serviceCollection.AddSingleton<DeployableFilesManager>();
        serviceCollection.AddSingleton<AssociatedFileEditorsManager>();
        serviceCollection.AddSingleton<RunningGamesManager>();

        // Using a WeakReferenceMessenger can be convenient and I was originally doing that,
        // but for performance reasons it's still better to always unregister (to avoid
        // dead objects still receiving messages in a sort of zombie state), so I've opted
        // for using the StrongReferenceMessenger for now. Can always be changed later.
        serviceCollection.AddSingleton<IMessenger, StrongReferenceMessenger>();

        // Add the main window
        serviceCollection.AddSingleton<MainWindow>();
        serviceCollection.AddSingleton<MainWindowViewModel>();

        // Add the pages
        serviceCollection.AddSingleton<GamesPageViewModel>();
        serviceCollection.AddSingleton<ProgressionPageViewModel>();
        serviceCollection.AddSingleton<UtilitiesPageViewModel>();
        serviceCollection.AddSingleton<SettingsPageViewModel>();
        serviceCollection.AddSingleton<AboutPageViewModel>();
        serviceCollection.AddSingleton<DebugPageViewModel>();

        // Add view models
        serviceCollection.AddSingleton<NewsViewModel>();

        // Add UI managers
        serviceCollection.AddSingleton<IDialogBaseManager, RCPWindowDialogBaseManager>();
        serviceCollection.AddSingleton<IMessageUIManager, RCPMessageUIManager>();
        serviceCollection.AddSingleton<IBrowseUIManager, RCPBrowseUIManager>();
        serviceCollection.AddSingleton<AppUIManager>();
    }
}