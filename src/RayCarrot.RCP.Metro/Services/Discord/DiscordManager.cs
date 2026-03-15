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

    private CancellationTokenSource? CancellationTokenSource { get; set; }
    private string? RunningGameInstallationId { get; set; }
    private GameRichPresenceManager? RunningGameRichPresenceManager { get; set; }

    #endregion

    #region Public Properties

    public TimeSpan RichPresenceCheckInterval { get; set; } = TimeSpan.FromSeconds(2);
    public bool IsInitialized => DiscordClient.IsInitialized;

    #endregion

    #region Private Methods

    private async Task GameRichPresenceLoop(CancellationToken cancellationToken)
    {
        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                // Wait between each check
                await Task.Delay(RichPresenceCheckInterval, cancellationToken);

                // Make sure the game is still running
                if (RunningGameRichPresenceManager == null || RunningGameRichPresenceManager.Process.HasExited)
                    return;

                // TODO-UPDATE: Handle exceptions
                // Get the current game presence
                string? presence = RunningGameRichPresenceManager?.GetPresence();

                // Update the presence if it has changed
                if (DiscordClient.CurrentPresence.State != presence)
                    DiscordClient.UpdateState(presence);
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
        Process process = Process.GetProcessById(pid); // TODO-UPDATE: This might throw an exception

        // Set the game presence
        DiscordClient.SetPresence(new RichPresence()
        {
            Details = $"Playing {component.DisplayName}",
            Assets = new DiscordRPC.Assets()
            {
                LargeImageKey = component.ImageKey,
                SmallImageKey = MainSmallImageKey,
            },
            Timestamps = new Timestamps(process.StartTime.ToUniversalTime())
        });

        // Try and get a rich presence manager for showing the current game context, like the level you're in
        RichPresenceManagerComponent? richPresenceComponent = gameInstallation.GetComponent<RichPresenceManagerComponent>();
        if (richPresenceComponent != null)
        {
            // Create a new manager instance
            RunningGameRichPresenceManager = richPresenceComponent.CreateObject(process);
            
            // Create a cancellation token source for this loop
            CancellationTokenSource = new CancellationTokenSource();

            // Start the loop
            Task.Run(() => GameRichPresenceLoop(CancellationTokenSource.Token)).WithoutAwait("Updating game rich presence");
        }
        else
        {
            process.Dispose();
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
            // TODO-UPDATE: Only get this once - also might throw exception
            Timestamps = new Timestamps(Process.GetCurrentProcess().StartTime.ToUniversalTime())
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