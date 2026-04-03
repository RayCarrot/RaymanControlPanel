namespace RayCarrot.RCP.Metro.Games.Clients;

public abstract class EmulatorGameClientDescriptor : GameClientDescriptor
{
    public override bool RequiresAccountVerification => false;

    /// <summary>
    /// The game platforms which this emulator supports
    /// </summary>
    public abstract GamePlatform[] SupportedPlatforms { get; }

    public override bool SupportsGame(GameInstallation gameInstallation, GameClientInstallation gameClientInstallation) =>
        base.SupportsGame(gameInstallation, gameClientInstallation) && 
        SupportedPlatforms.Contains(gameInstallation.GameDescriptor.Platform);
}