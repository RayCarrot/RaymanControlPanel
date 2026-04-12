using System.IO;
using BinarySerializer.Bakesale;
using BinarySerializer.PlayStation.PS1.MemoryCard;
using BinarySerializer.Ray1;

namespace RayCarrot.RCP.Metro;

public class GameProgressionManager_Rayman30thAnniversaryEdition_Ps1_Win32 : GameProgressionManager_Rayman30thAnniversaryEdition_Win32
{
    public GameProgressionManager_Rayman30thAnniversaryEdition_Ps1_Win32(GameDescriptor_Rayman30thAnniversaryEdition_Win32 gameDescriptor, GameInstallation gameInstallation, string progressionId) 
        : base(gameDescriptor, gameInstallation, progressionId) { }

    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    public override string Name => "PlayStation"; // TODO-LOC?
    public override GameBackups_Directory[] BackupDirectories => new GameBackups_Directory[]
    {
        new(GameDescriptor.GetSaveDirectory(GameInstallation), SearchOption.TopDirectoryOnly, "ps1_rayman.bsav?", "0", 0),
    };

    public override async IAsyncEnumerable<GameProgressionSlot> LoadSlotsAsync(FileSystemWrapper fileSystem)
    {
        // Get the save directory
        FileSystemPath? saveDir = fileSystem.GetDirectory(new IOSearchPattern(GameDescriptor.GetSaveDirectory(GameInstallation), SearchOption.TopDirectoryOnly, "ps1_rayman.bsav*"))?.DirPath;

        if (saveDir == null)
            yield break;

        // Create the context
        using RCPContext context = new(saveDir);
        Ray1Settings settings = new(Ray1EngineVersion.PS1);
        context.AddSettings(settings);

        // Check every save (the collection save states)
        for (int saveIndex = 0; saveIndex < SavesPerGame; saveIndex++)
        {
            string fileName = $"ps1_rayman.bsav{saveIndex}";

            Logger.Info("{0} save {1} is being loaded...", GameInstallation.FullId, saveIndex);

            // Load the save
            BakesaleSaveFile<PancakeMemoryCard<SaveSlot>>? saveData = await context.ReadFileDataAsync<BakesaleSaveFile<PancakeMemoryCard<SaveSlot>>>(fileName, removeFileWhenComplete: false);

            if (saveData == null || saveData.SaveData == null)
            {
                Logger.Info("{0} save {1} was not found", GameInstallation.FullId, saveIndex);
                continue;
            }

            Logger.Info("{0} save {1} has been deserialized", GameInstallation.FullId, saveIndex);

            // Load every in-game save slot
            for (var dataBlockIndex = 0; dataBlockIndex < saveData.SaveData.DataBlocks.Length; dataBlockIndex++)
            {
                DataBlock<SaveSlot> dataBlock = saveData.SaveData.DataBlocks[dataBlockIndex];
                if (dataBlock == null)
                    continue;

                IReadOnlyList<GameProgressionDataItem> dataItems = Rayman1Progression.CreateProgressionItems(
                    dataBlock.SaveData, out int collectiblesCount, out int maxCollectiblesCount);

                int index = dataBlockIndex;
                yield return new SerializableGameProgressionSlot<BakesaleSaveFile<PancakeMemoryCard<SaveSlot>>>(
                    name: saveData.SaveData.Directories[dataBlockIndex].Identifier[..3].ToUpper(),
                    index: -1,
                    collectiblesCount: collectiblesCount,
                    totalCollectiblesCount: maxCollectiblesCount,
                    dataItems: dataItems,
                    context: context,
                    serializable: saveData,
                    fileName: fileName)
                {
                    GetExportObject = x => x.SaveData.DataBlocks[index].SaveData,
                    SetImportObject = (x, o) => x.SaveData.DataBlocks[index].SaveData = (SaveSlot)o,
                    ExportedType = typeof(SaveSlot),
                };
            }
        }
    }
}