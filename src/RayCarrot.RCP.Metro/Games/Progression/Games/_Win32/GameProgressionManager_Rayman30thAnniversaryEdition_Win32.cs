namespace RayCarrot.RCP.Metro;

public abstract class GameProgressionManager_Rayman30thAnniversaryEdition_Win32 : GameProgressionManager
{
    protected GameProgressionManager_Rayman30thAnniversaryEdition_Win32(GameDescriptor_Rayman30thAnniversaryEdition_Win32 gameDescriptor, GameInstallation gameInstallation, string progressionId) 
        : base(gameInstallation, progressionId)
    {
        GameDescriptor = gameDescriptor;
    }

    protected const int SavesPerGame = 3;

    public GameDescriptor_Rayman30thAnniversaryEdition_Win32 GameDescriptor { get; }
}