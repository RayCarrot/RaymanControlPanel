using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Threading;
using Microsoft.Extensions.DependencyInjection;
using NLog.Targets;

namespace RayCarrot.RCP.Metro;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    #region Constructor

    public App(IServiceProvider serviceProvider)
    {
        ServiceProvider = serviceProvider;

        // Set services
        AppViewModel = ServiceProvider.GetRequiredService<AppViewModel>();
        Data = ServiceProvider.GetRequiredService<AppUserData>();

        // Subscribe to events
        Startup += App_Startup;
        DispatcherUnhandledException += App_DispatcherUnhandledException;
        Exit += App_Exit;

        try
        {
            Assembly? entry = Assembly.GetEntryAssembly();

            if (entry is null)
                throw new InvalidOperationException("The application can not use a Mutex for forcing a single instance if no valid entry assembly is found");

            // Use mutex to only allow one instance of the application at a time
            Mutex = new Mutex(false, "Global\\" + ((GuidAttribute)entry.GetCustomAttributes(typeof(GuidAttribute), false).GetValue(0)).Value);
        }
        catch (IndexOutOfRangeException ex)
        {
            throw new InvalidOperationException("The application can not use a Mutex for forcing a single instance if the entry assembly does not have a valid GUID identifier", ex);
        }
    }

    #endregion

    #region Logger

    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    #endregion

    #region Services

    private AppViewModel AppViewModel { get; }
    private AppUserData Data { get; }

    #endregion

    #region Private Properties

    /// <summary>
    /// The mutex
    /// </summary>m
    private Mutex Mutex { get; }

    /// <summary>
    /// Indicates if the main window is currently closing
    /// </summary>
    private bool IsClosing { get; set; }

    #endregion

    #region Public Properties

    // TODO: Attempt to limit the amount of times we access this. The times we need it are
    //       mainly for the main window or dispatcher. Find better solutions?
    public new static App Current => Application.Current as App ?? throw new InvalidOperationException($"Current app is not a valid {nameof(App)}");

    /// <summary>
    /// The service provider for this application
    /// </summary>
    public IServiceProvider ServiceProvider { get; }

    public MainWindow? ChildWindowsParent => MainWindow as MainWindow;

    #endregion

    #region Event Handlers

    private async void App_Startup(object sender, StartupEventArgs e)
    {
        // Set the shutdown mode to be explicit. If not the app
        // will automatically shut down when the first window
        // closes (which might be the license one).
        ShutdownMode = ShutdownMode.OnExplicitShutdown;

        // Verify single instance
        if (!VerifySingleInstance())
        {
            Shutdown();
            return;
        }

        // Verify runtime
        if (!VerifyRuntime())
        {
            Shutdown();
            return;
        }

        // Verify license
        if (!VerifyLicense(ServiceProvider.GetRequiredService<LicenseManager>()))
        {
            Shutdown();
            return;
        }

        StartupManager startupManager = ServiceProvider.GetRequiredService<StartupManager>();
        await startupManager.RunAsync(true, ServiceProvider.GetRequiredService<MainWindow>);
    }

    private void App_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        try
        {
            // Log the exception
            Logger.Fatal(e.Exception, "Unhandled exception");

            // Get the path to log to
            string logPath = Path.Combine(Directory.GetCurrentDirectory(), "crashlog.txt");

            // Write log
            File.WriteAllLines(logPath, LogManager.Configuration.FindTargetByName<MemoryTarget>("memory")?.Logs ?? new string[]
            {
                "Service not available",
                Environment.NewLine,
                e.Exception?.ToString() ?? "<No Exception>"
            });

            // Notify user
            MessageBox.Show($"The application crashed with the following exception message:{Environment.NewLine}{e.Exception?.Message}" +
                            $"{Environment.NewLine}{Environment.NewLine}{Environment.NewLine}A crash log has been created under {logPath}.",
                "Critical error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        catch (Exception)
        {
            // Notify user
            MessageBox.Show($"The application crashed with the following exception message:{Environment.NewLine}{e.Exception?.Message}",
                "Critical error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
        finally
        {
            // Dispose
            Dispose();

            // Close the logger
            LogManager.Shutdown();

            if (MainWindow is MainWindow m)
                m.ViewModel.Dispose();
        }
    }

    private void App_Exit(object sender, ExitEventArgs e)
    {
        // Dispose
        Dispose();
    }

    #endregion

    #region Private Methods

    private bool VerifySingleInstance()
    {
        try
        {
            if (!Mutex.WaitOne(0, false))
            {
                // Attempt to find the other running process
                using Process? otherProcess = TryGetOtherInstanceProcess();

                bool showError = false;

                // A process was found
                if (otherProcess != null)
                {
                    LaunchArguments args = ServiceProvider.GetRequiredService<LaunchArguments>();

                    // Check if RCP is being restarted. If so we want to wait for the previous instance to close.
                    if (args.HasArg("-restart"))
                    {
                        // Wait a maximum of 5 seconds for exit
                        bool exited = otherProcess.WaitForExit(5000);

                        if (exited)
                            return true;

                        showError = true;
                    }
                    // If not restarting we direct the user to the currently running instance
                    else
                    {
                        // Focus the process
                        try
                        {
                            WindowsHelpers.BringWindowToFront(otherProcess.MainWindowHandle);
                        }
                        catch
                        {
                            showError = true;
                        }

                        // If there are launch arguments we try to send those over to the running instance
                        try
                        {
                            if (args.HasArgs)
                                args.SendArguments();
                        }
                        catch
                        {
                            showError = true;
                        }
                    }
                }
                else
                {
                    showError = true;
                }

                if (showError)
                    MessageBox.Show("An instance of the Rayman Control Panel is already running", "Error starting",
                        MessageBoxButton.OK, MessageBoxImage.Error);

                return false;
            }
        }
        catch (AbandonedMutexException)
        {
            // Break if debugging
            Debugger.Break();
        }

        return true;
    }

    private Process? TryGetOtherInstanceProcess()
    {
        try
        {
            using Process currentProcess = Process.GetCurrentProcess();
            string currentProcessName = currentProcess.ProcessName;
            int currentProcessID = currentProcess.Id;

            // NOTE: This will only work if the processes were opened from the same file (or another file with the same name).
            //       Find better solution?
            // Attempt to find the other running process
            return Process.GetProcessesByName(currentProcessName).FirstOrDefault(x => x.Id != currentProcessID);
        }
        catch
        {
            return null;
        }
    }

    private bool VerifyRuntime()
    {
        // Make sure we are on Windows Vista or higher for APIs such as the
        // Windows API Code Pack and Deployment Image Servicing and Management
        if (AppViewModel.WindowsVersion < WindowsVersion.WinVista && AppViewModel.WindowsVersion != WindowsVersion.Unknown)
        {
            MessageBox.Show("Windows Vista or higher is required to run this application", "Error starting",
                MessageBoxButton.OK, MessageBoxImage.Error);
            return false;
        }

        return true;
    }

    private bool VerifyLicense(LicenseManager licenseManager)
    {
        try
        {
            if (licenseManager.HasAcceptedLicense())
                return true;

            // Show the license prompt
            return licenseManager.PrompLicense();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"The license verification failed with the message of: {ex.Message}", "License error", MessageBoxButton.OK, MessageBoxImage.Error);

            return false;
        }
    }

    private void Dispose() => Mutex.Dispose();

    #endregion

    #region Public Methods

    /// <summary>
    /// Shuts down the application
    /// </summary>
    /// <param name="forceShutDown">Indicates if the app should be forced to shut down</param>
    /// <returns>The task</returns>
    public async Task ShutdownAppAsync(bool forceShutDown)
    {
        // If already is closing, ignore
        if (IsClosing)
            return;

        // Flag that we are closing
        IsClosing = true;

        try
        {
            await await Dispatcher.InvokeAsync(async () =>
            {
                // Don't close if loading
                if (AppViewModel.LoaderViewModel.IsRunning)
                    return;

                // Attempt to close all windows except the main one
                foreach (Window window in Windows.Cast<Window>().ToArray())
                {
                    // Ignore the main window for now
                    if (window == MainWindow)
                        continue;

                    // Focus the window
                    window.Focus();

                    // Attempt to close the window
                    window.Close();
                }

                // Attempt to close all child windows, starting with the modal ones
                foreach (RCPChildWindow childWindow in ChildWindowInstance.OpenChildWindows.OrderBy(x => x.IsModal ? 0 : 1).ToArray())
                {
                    if (childWindow is { IsMinimized: true } c)
                        c.ToggleMinimized();

                    childWindow.Close();
                }

                // Yield so that the child windows fully close before we do the next check
                await Dispatcher.Yield(DispatcherPriority.ApplicationIdle);

                // Make sure all other windows have been closed unless forcing a shut down
                if (!forceShutDown && (Windows.Count > 1 || ChildWindowInstance.OpenChildWindows.Any()))
                {
                    Logger.Info("The shutdown was canceled due to one or more windows still being open");

                    IsClosing = false;
                    return;
                }

                // Save window state
                if (MainWindow != null)
                    Data.UI_WindowState = WindowSessionState.GetWindowState(MainWindow);

                Logger.Info("The application is exiting...");

                // Dispose
                if (MainWindow is MainWindow m)
                    m.ViewModel.Dispose();

                // Stop checking for running games
                ServiceProvider.GetRequiredService<RunningGamesManager>().Stop();

                // Save all user data
                ServiceProvider.GetRequiredService<AppDataManager>().Save();

                // Close the logger
                LogManager.Shutdown();

                // Shut down application
                Shutdown();
            });
        }
        catch (Exception ex)
        {
            try
            {
                await Dispatcher.InvokeAsync(() =>
                {
                    // Attempt to log the exception, ignoring any exceptions thrown
                    try
                    {
                        Logger.Fatal(ex, "Shutting down app");
                    }
                    catch
                    {
                        // Ignored
                    }

                    // Notify the user of the error
                    MessageBox.Show($"An error occurred when shutting down the application. Error message: {ex.Message}", "Error",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                });
            }
            finally
            {
                // Close application
                Shutdown();
            }
        }
    }

    #endregion
}