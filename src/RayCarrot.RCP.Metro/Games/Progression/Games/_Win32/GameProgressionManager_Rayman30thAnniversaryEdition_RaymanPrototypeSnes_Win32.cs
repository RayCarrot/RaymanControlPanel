namespace RayCarrot.RCP.Metro;

public class GameProgressionManager_Rayman30thAnniversaryEdition_RaymanPrototypeSnes_Win32 : GameProgressionManager_Rayman30thAnniversaryEdition_Win32
{
    public GameProgressionManager_Rayman30thAnniversaryEdition_RaymanPrototypeSnes_Win32(GameDescriptor_Rayman30thAnniversaryEdition_Win32 gameDescriptor, GameInstallation gameInstallation, string progressionId, string gameId) 
        : base(gameDescriptor, gameInstallation, progressionId, gameId) { }

    public override string Name => "Rayman Prototype - SNES"; // TODO-LOC?
}