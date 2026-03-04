namespace RayCarrot.RCP.Metro;

public record GameRunningChangedMessage(GameInstallation GameInstallation, bool IsRunning);