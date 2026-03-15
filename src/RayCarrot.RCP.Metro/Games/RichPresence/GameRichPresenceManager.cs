using System.Diagnostics;

namespace RayCarrot.RCP.Metro.Games.RichPresence;

public abstract class GameRichPresenceManager : IDisposable
{
    protected GameRichPresenceManager(GameInstallation gameInstallation, Process process)
    {
        GameInstallation = gameInstallation;
        Process = process;
        Reader = new ProcessMemoryReader(process);
    }

    public GameInstallation GameInstallation { get; }
    public Process Process { get; }
    public ProcessMemoryReader Reader { get; }

    public abstract string? GetPresence();

    public void Dispose()
    {
        Process.Dispose();
        Reader.Dispose();
    }
}