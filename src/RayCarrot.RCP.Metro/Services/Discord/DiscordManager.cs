using System.Diagnostics;
using DiscordRPC;
using RayCarrot.RCP.Metro.Games.Components;
using RayCarrot.RCP.Metro.Games.RichPresence;

namespace RayCarrot.RCP.Metro;

public class DiscordManager : IDisposable, IRecipient<GameRunningChangedMessage>
{
    #region Constructor

    public DiscordManager(IMessenger messenger, RunningGamesManager runningGamesManager, AppUserData data)
    {
        // Set services
        Messenger = messenger ?? throw new ArgumentNullException(nameof(messenger));
        RunningGamesManager = runningGamesManager ?? throw new ArgumentNullException(nameof(runningGamesManager));
        Data = data ?? throw new ArgumentNullException(nameof(data));

        // Register for messages
        Messenger.RegisterAll(this);

        // Create properties
        DiscordClient = new DiscordRpcClient(AppId, logger: new DiscordLogger());
        CancellationTokenSource = new CancellationTokenSource();

        try
        {
            // Try and get the current process start time
            using Process currentProcess = Process.GetCurrentProcess();
            IdleStartTime = currentProcess.StartTime.ToUniversalTime();
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Getting current process start time");

            // Fallback to the current time
            IdleStartTime = DateTime.UtcNow;
        }
    }

    #endregion

    #region Constant Fields

    private const string AppId = "1478792907862446182";
    private const string MainSmallImageKey = "rcp";

    #endregion

    #region Logger

    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    #endregion

    #region Private Properties

    private IMessenger Messenger { get; }
    private RunningGamesManager RunningGamesManager { get; }
    private AppUserData Data { get; }
    private DiscordRpcClient DiscordClient { get; }
    private DateTime IdleStartTime { get; }

    private CancellationTokenSource? CancellationTokenSource { get; set; }
    private string? RunningGameInstallationId { get; set; }
    private GameRichPresenceManager? RunningGameRichPresenceManager { get; set; }

    #endregion

    #region Public Properties

    public TimeSpan RichPresenceCheckInterval { get; set; } = TimeSpan.FromSeconds(5);
    public bool IsInitialized => DiscordClient.IsInitialized;

    #endregion

    #region Private Methods

    private async Task GameRichPresenceLoop(CancellationToken cancellationToken)
    {
        try
        {
            int retryCount = 0;

            while (!cancellationToken.IsCancellationRequested)
            {
                // Wait between each check
                await Task.Delay(RichPresenceCheckInterval, cancellationToken);

                // Make sure the game is still running
                GameRichPresenceManager? manager = RunningGameRichPresenceManager;
                if (manager == null || manager.Process.HasExited)
                    return;

                try
                {
                    // Get the current game presence
                    string? presence = manager.GetPresence();

                    // Update the presence if it has changed
                    if (DiscordClient.CurrentPresence.State != presence)
                        DiscordClient.UpdateState(presence);

                    retryCount = 0;
                }
                catch (Exception ex)
                {
                    Logger.Warn(ex, "Getting game presence");
                    retryCount++;

                    // Don't retry more than 10 times
                    if (retryCount > 10)
                        return;
                }
            }
        }
        catch (TaskCanceledException)
        {
            // Do nothing
        }
    }

    private void StopCurrentGameRichPresenceLoop()
    {
        // Cancel the loop and dispose the token source
        CancellationTokenSource?.Cancel();
        CancellationTokenSource?.Dispose();
        CancellationTokenSource = null;

        // Dispose and remove the manager
        RunningGameRichPresenceManager?.Dispose();
        RunningGameRichPresenceManager = null;

        Logger.Info("Stopped the current game rich presence loop");
    }

    #endregion

    #region Public Methods

    public void Initialize()
    {
        // Don't initialize if Discord Rich Presence is disabled or if already initialized
        if (!Data.App_UseDiscordRichPresence || IsInitialized)
            return;

        try
        {
            // Try to initialize
            if (DiscordClient.Initialize())
            {
                Logger.Info("Initialized Discord Rich Presence");
                
                // Set the default presence
                SetDefaultPresence();
            }
            else
            {
                Logger.Info("Failed to initialize Discord Rich Presence");
            }
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Connecting to Discord client");
        }
    }

    public void Uninitialize()
    {
        StopCurrentGameRichPresenceLoop();

        // Don't uninitialize if not initialized
        if (!IsInitialized) 
            return;
        
        DiscordClient.ClearPresence();
        DiscordClient.Deinitialize();
        Logger.Info("Uninitialized Discord Rich Presence");
    }

    public void SetGamePlayingPresence(GameInstallation gameInstallation)
    {
        // Try and get the component
        DiscordRichPresenceComponent? component = gameInstallation.GetComponent<DiscordRichPresenceComponent>();

        // If there's no component then the game doesn't support Discord Rich Presence and we revert to the idle presence
        if (component == null)
        {
            Logger.Info("Couldn't set Discord Rich Presence for game {0} due to it not having the component registered", gameInstallation.FullId);
            SetIdlePresence();
            return;
        }

        // Stop any previous rich presence loop
        StopCurrentGameRichPresenceLoop();

        // Set the new game installation ID
        RunningGameInstallationId = gameInstallation.InstallationId;

        // Get the game process
        int pid = RunningGamesManager.GetProcessId(gameInstallation);
        Process? process = null;
        DateTime startTime = DateTime.UtcNow;
        try
        {
            process = Process.GetProcessById(pid);
            startTime = process.StartTime.ToUniversalTime();
        }
        catch (Exception ex)
        {
            Logger.Warn(ex, "Getting game process");
        }
        
        // Set the game presence
        DiscordClient.SetPresence(new RichPresence()
        {
            Details = $"Playing {component.DisplayName}",
            Assets = new DiscordRPC.Assets()
            {
                LargeImageKey = component.ImageKey,
                SmallImageKey = MainSmallImageKey,
            },
            Timestamps = new Timestamps(startTime)
        });

        // Try and get a rich presence manager for showing the current game context, like the level you're in
        RichPresenceManagerComponent? richPresenceComponent = gameInstallation.GetComponent<RichPresenceManagerComponent>();
        if (process != null && richPresenceComponent != null)
        {
            // Create a new manager instance
            RunningGameRichPresenceManager = richPresenceComponent.CreateObject(process);
            
            // Create a cancellation token source for this loop
            CancellationTokenSource tokenSource = new();
            CancellationTokenSource = tokenSource;

            // Start the loop
            Task.Run(() => GameRichPresenceLoop(tokenSource.Token)).WithoutAwait("Updating game rich presence");

            Logger.Info("Started the game rich presence loop");
        }
        else
        {
            process?.Dispose();
        }

        Logger.Info("Set Discord Rich Presence for game {0}", gameInstallation.FullId);
    }

    public void SetIdlePresence()
    {
        // Stop any previous rich presence loop and clear the game installation ID
        StopCurrentGameRichPresenceLoop();
        RunningGameInstallationId = null;
        
        // Set the idle presence
        DiscordClient.SetPresence(new RichPresence()
        {
            Timestamps = new Timestamps(IdleStartTime)
        });

        Logger.Info("Set the idle Discord Rich Presence");
    }

    public void SetDefaultPresence()
    {
        // Set the default presence, which is either a playing game if one is running, or the idle presence
        GameInstallation[] runningGames = RunningGamesManager.GetRunningGames();
        if (runningGames.Length > 0)
            SetGamePlayingPresence(runningGames[0]);
        else
            SetIdlePresence();
    }

    public void Dispose()
    {
        StopCurrentGameRichPresenceLoop();
        if (DiscordClient.IsInitialized)
            DiscordClient.ClearPresence();
        DiscordClient.Dispose();
        Messenger.UnregisterAll(this);
    }

    #endregion

    #region Message Handlers

    void IRecipient<GameRunningChangedMessage>.Receive(GameRunningChangedMessage message)
    {
        if (!Data.App_UseDiscordRichPresence)
            return;

        // Started running
        if (message.IsRunning)
        {
            SetGamePlayingPresence(message.GameInstallation);
        }
        // Stopped running
        else
        {
            if (RunningGameInstallationId == message.GameInstallation.InstallationId)
                SetDefaultPresence();
        }
    }

    #endregion

    #region Data Types

#pragma warning disable CA2254
    private class DiscordLogger : DiscordRPC.Logging.ILogger
    {
        public DiscordRPC.Logging.LogLevel Level { get; set; } = DiscordRPC.Logging.LogLevel.Trace;

        public void Trace(string message, params object[] args)
        {
            Logger.Trace(message, args);
        }

        public void Info(string message, params object[] args)
        {
            Logger.Info(message, args);
        }

        public void Warning(string message, params object[] args)
        {
            Logger.Warn(message, args);
        }

        public void Error(string message, params object[] args)
        {
            Logger.Error(message, args);
        }
    }

    #endregion
}