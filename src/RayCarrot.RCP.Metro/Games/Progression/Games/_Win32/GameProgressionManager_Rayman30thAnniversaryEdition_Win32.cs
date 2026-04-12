using System.IO;

namespace RayCarrot.RCP.Metro;

public abstract class GameProgressionManager_Rayman30thAnniversaryEdition_Win32 : GameProgressionManager
{
    protected GameProgressionManager_Rayman30thAnniversaryEdition_Win32(GameDescriptor_Rayman30thAnniversaryEdition_Win32 gameDescriptor, GameInstallation gameInstallation, string progressionId, string gameId) 
        : base(gameInstallation, $"{progressionId} - {gameId}")
    {
        GameDescriptor = gameDescriptor;
        GameId = gameId;
    }

    protected const int SavesPerGame = 3;

    public GameDescriptor_Rayman30thAnniversaryEdition_Win32 GameDescriptor { get; }
    public string GameId { get; }

    public override GameBackups_Directory[] BackupDirectories
    {
        get
        {
            FileSystemPath saveDir = GameDescriptor.GetSaveDirectory(GameInstallation);
            return
            [
                new(saveDir, SearchOption.TopDirectoryOnly, $"{GameId}.bsav?", "0", 0),
                new(saveDir, SearchOption.TopDirectoryOnly, $"{GameId}.bst?", "1", 0)
            ];
        }
    }
}