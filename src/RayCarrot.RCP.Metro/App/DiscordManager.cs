using System.Diagnostics;
using DiscordRPC;
using RayCarrot.RCP.Metro.Games.Components;
using RayCarrot.RCP.Metro.Games.RichPresence;

namespace RayCarrot.RCP.Metro;

public class DiscordManager : IDisposable, IRecipient<GameRunningChangedMessage>
{
    public DiscordManager(IMessenger messenger, RunningGamesManager runningGamesManager, AppUserData data)
    {
        Messenger = messenger ?? throw new ArgumentNullException(nameof(messenger));
        RunningGamesManager = runningGamesManager ?? throw new ArgumentNullException(nameof(runningGamesManager));
        Data = data ?? throw new ArgumentNullException(nameof(data));

        Messenger.RegisterAll(this);

        DiscordClient = new DiscordRpcClient(AppId, logger: new DiscordLogger());
        CancellationTokenSource = new CancellationTokenSource();
    }

    private const string AppId = "1478792907862446182";
    private const string MainSmallImageKey = "rcp";

    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    private IMessenger Messenger { get; }
    private RunningGamesManager RunningGamesManager { get; }
    private AppUserData Data { get; }
    private DiscordRpcClient DiscordClient { get; }
    private CancellationTokenSource? CancellationTokenSource { get; set; }
    private string? RunningGameInstallationId { get; set; }
    private GameRichPresenceManager? RunningGameRichPresenceManager { get; set; }

    public TimeSpan RichPresenceCheckInterval { get; set; } = TimeSpan.FromSeconds(2);

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
        RunningGameRichPresenceManager?.Dispose();
        RunningGameRichPresenceManager = null;

        CancellationTokenSource?.Cancel();
        CancellationTokenSource?.Dispose();
        CancellationTokenSource = null;
    }

    public void Initialize()
    {
        if (!Data.App_UseDiscordRichPresence || DiscordClient.IsInitialized)
            return;

        try
        {
            if (DiscordClient.Initialize())
            {
                Logger.Info("Initialized Discord Rich Presence");
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
        if (DiscordClient.IsInitialized)
        {
            StopCurrentGameRichPresenceLoop();
            DiscordClient.ClearPresence();
            DiscordClient.Deinitialize();
            Logger.Info("Uninitialized Discord Rich Presence");
        }
    }

    public void SetGamePlayingPresence(GameInstallation gameInstallation)
    {
        DiscordRichPresenceComponent? component = gameInstallation.GetComponent<DiscordRichPresenceComponent>();

        if (component == null)
        {
            Logger.Info("Couldn't set Discord Rich Presence for game {0} due to it not having the component registered", gameInstallation.FullId);
            SetIdlePresence();
            return;
        }

        StopCurrentGameRichPresenceLoop();
        RunningGameInstallationId = gameInstallation.InstallationId;

        int pid = RunningGamesManager.GetProcessId(gameInstallation);
        using Process process = Process.GetProcessById(pid); // TODO-UPDATE: This might throw an exception

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

        RichPresenceManagerComponent? richPresenceComponent = gameInstallation.GetComponent<RichPresenceManagerComponent>();
        if (richPresenceComponent != null)
        {
            RunningGameRichPresenceManager = richPresenceComponent.CreateObject(process);
            CancellationTokenSource = new CancellationTokenSource();
            Task.Run(() => GameRichPresenceLoop(CancellationTokenSource.Token)).WithoutAwait("Updating game rich presence");
        }

        Logger.Info("Set Discord Rich Presence for game {0}", gameInstallation.FullId);
    }

    public void SetIdlePresence()
    {
        StopCurrentGameRichPresenceLoop();
        RunningGameInstallationId = null;
        
        DiscordClient.SetPresence(new RichPresence()
        {
            // TODO-UPDATE: Only get this once - also might throw exception
            Timestamps = new Timestamps(Process.GetCurrentProcess().StartTime.ToUniversalTime())
        });
        Logger.Info("Set the idle Discord Rich Presence");
    }

    public void SetDefaultPresence()
    {
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
}