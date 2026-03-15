using System.Diagnostics;
using DiscordRPC;
using RayCarrot.RCP.Metro.Games.Components;

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
    private CancellationTokenSource CancellationTokenSource { get; }
    private string? RunningGameInstallationId { get; set; }

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

        RunningGameInstallationId = gameInstallation.InstallationId;

        Logger.Info("Set Discord Rich Presence for game {0}", gameInstallation.FullId);
    }

    public void SetIdlePresence()
    {
        RunningGameInstallationId = null;
        
        DiscordClient.SetPresence(new RichPresence()
        {
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
        CancellationTokenSource.Cancel();
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