using System.IO;
using BinarySerializer.Bakesale;
using BinarySerializer.PlayStation.PS1.MemoryCard;
using BinarySerializer.Ray1;

namespace RayCarrot.RCP.Metro;

public class GameProgressionManager_Rayman30thAnniversaryEdition_RaymanPs1_Win32 : GameProgressionManager_Rayman30thAnniversaryEdition_Win32
{
    public GameProgressionManager_Rayman30thAnniversaryEdition_RaymanPs1_Win32(GameDescriptor_Rayman30thAnniversaryEdition_Win32 gameDescriptor, GameInstallation gameInstallation, string progressionId, string gameId) 
        : base(gameDescriptor, gameInstallation, progressionId, gameId) { }

    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    public override string Name => "Rayman - PlayStation"; // TODO-LOC?

    public override async IAsyncEnumerable<GameProgressionSlot> LoadSlotsAsync(FileSystemWrapper fileSystem)
    {
        // Get the save directory
        FileSystemPath? saveDir = fileSystem.GetDirectory(new IOSearchPattern(GameDescriptor.GetSaveDirectory(GameInstallation), SearchOption.TopDirectoryOnly, $"{GameId}.bsav?"))?.DirPath;

        if (saveDir == null)
            yield break;

        // Create the context
        using RCPContext context = new(saveDir);
        Ray1Settings settings = new(Ray1EngineVersion.PS1);
        context.AddSettings(settings);

        // Check every save (the collection save states)
        for (int saveIndex = 0; saveIndex < SavesPerGame; saveIndex++)
        {
            string fileName = $"{GameId}.bsav{saveIndex}";

            Logger.Info("{0} save {1} is being loaded...", GameInstallation.FullId, saveIndex);

            // Load the save
            BakesaleSaveFile<PancakeMemoryCard<SaveSlot>>? saveData = await context.ReadFileDataAsync<BakesaleSaveFile<PancakeMemoryCard<SaveSlot>>>(fileName, removeFileWhenComplete: false);

            if (saveData?.SaveData == null)
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