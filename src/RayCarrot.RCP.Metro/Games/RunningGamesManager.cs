using System.Diagnostics;
using System.IO;
using RayCarrot.RCP.Metro.Games.Structure;

namespace RayCarrot.RCP.Metro;

public class RunningGamesManager
{
    public RunningGamesManager(GamesManager gamesManager, IMessenger messenger)
    {
        GamesManager = gamesManager ?? throw new ArgumentNullException(nameof(gamesManager));
        Messenger = messenger ?? throw new ArgumentNullException(nameof(messenger));
    }

    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    private GamesManager GamesManager { get; }
    private IMessenger Messenger { get; }
    private List<GameInstallation> RunningGames { get; } = new();
    private CancellationTokenSource? CancellationTokenSource { get; set; }

    public TimeSpan CheckInterval { get; set; } = TimeSpan.FromSeconds(2);

    private async Task GameCheckLoop(CancellationToken cancellationToken)
    {
        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                // Wait between each check
                await Task.Delay(CheckInterval, cancellationToken);

                // Check the running games
                CheckGames();
            }
        }
        catch (TaskCanceledException)
        {
            // Do nothing
        }
    }

    private void CheckGames()
    {
        foreach (GameInstallation gameInstallation in GamesManager.GetInstalledGames())
        {
            try
            {
                bool isRunning = false;

                // Get the exe file path
                FileSystemPath exeFilePath;
                if (gameInstallation.InstallLocation.HasFile)
                    exeFilePath = gameInstallation.InstallLocation.FilePath;
                else if (gameInstallation.GameDescriptor.Structure is DirectoryProgramInstallationStructure dirStructure)
                    exeFilePath = dirStructure.FileSystem.GetAbsolutePath(gameInstallation, ProgramPathType.PrimaryExe);
                else
                    continue;

                // Get the process name
                string processName = Path.GetFileNameWithoutExtension(exeFilePath);

                // Enumerate every process with that name
                foreach (Process process in Process.GetProcessesByName(processName))
                {
                    // Verify the path matches
                    if (process.MainModule?.FileName.Equals(exeFilePath.FullPath, StringComparison.OrdinalIgnoreCase) != true)
                        continue;

                    lock (RunningGames)
                    {
                        if (!RunningGames.Contains(gameInstallation))
                        {
                            Messenger.Send(new GameRunningChangedMessage(gameInstallation, true));
                            RunningGames.Add(gameInstallation);
                            Logger.Info("The game {0} has been detected as running in process {1}", gameInstallation.InstallationId, process.Id);
                        }
                    }

                    isRunning = true;
                    break;
                }

                if (!isRunning)
                {
                    lock (RunningGames)
                    {
                        if (RunningGames.Contains(gameInstallation))
                        {
                            Messenger.Send(new GameRunningChangedMessage(gameInstallation, false));
                            RunningGames.Remove(gameInstallation);
                            Logger.Info("The game {0} has been detected as no longer running", gameInstallation.InstallationId);
                        }
                    }
                }
            }
            catch
            {
                // Don't log since then it'd log too often
            }
        }
    }

    public void Start()
    {
        CancellationTokenSource = new CancellationTokenSource();
        GameCheckLoop(CancellationTokenSource.Token).WithoutAwait("Checking for running games");
    }

    public void Stop()
    {
        CancellationTokenSource?.Cancel();
    }

    public bool IsGameRunning(GameInstallation gameInstallation)
    {
        lock (RunningGames)
            return RunningGames.Contains(gameInstallation);
    }
}