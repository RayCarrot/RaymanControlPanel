using System.IO;
using BinarySerializer;
using BinarySerializer.Bakesale;
using BinarySerializer.Ray1;
using BinarySerializer.Ray1.Jaguar;

namespace RayCarrot.RCP.Metro;

public class GameProgressionManager_Rayman30thAnniversaryEdition_RaymanJaguar_Win32 : GameProgressionManager_Rayman30thAnniversaryEdition_Win32
{
    public GameProgressionManager_Rayman30thAnniversaryEdition_RaymanJaguar_Win32(GameDescriptor_Rayman30thAnniversaryEdition_Win32 gameDescriptor, GameInstallation gameInstallation, string progressionId, string gameId) 
        : base(gameDescriptor, gameInstallation, progressionId, gameId) { }

    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    public override string Name => "Rayman - Atari Jaguar"; // TODO-LOC?

    public override async IAsyncEnumerable<GameProgressionSlot> LoadSlotsAsync(FileSystemWrapper fileSystem)
    {
        // Get the save directory
        FileSystemPath? saveDir = fileSystem.GetDirectory(new IOSearchPattern(GameDescriptor.GetSaveDirectory(GameInstallation), SearchOption.TopDirectoryOnly, $"{GameId}.bst?"))?.DirPath;

        if (saveDir == null)
            yield break;

        // Create the context
        using RCPContext context = new(saveDir);
        Ray1Settings settings = new(Ray1EngineVersion.Jaguar);
        context.AddSettings(settings);

        // Check every save (the collection save states)
        for (int saveIndex = 0; saveIndex < SavesPerGame; saveIndex++)
        {
            string fileName = $"{GameId}.bst{saveIndex}";

            Logger.Info("{0} save {1} is being loaded...", GameInstallation.FullId, saveIndex);

            // Load the save
            BakesaleSaveState<Rayman30thJaguarSave>? saveData = await context.ReadFileDataAsync<BakesaleSaveState<Rayman30thJaguarSave>>(fileName, removeFileWhenComplete: false, recreateOnWrite: false);

            if (saveData?.SaveData == null)
            {
                Logger.Info("{0} save {1} was not found", GameInstallation.FullId, saveIndex);
                continue;
            }

            Logger.Info("{0} save {1} has been deserialized", GameInstallation.FullId, saveIndex);

            // Load every in-game save slot
            for (int slotIndex = 0; slotIndex < 3; slotIndex++)
            {
                JAG_SaveSlot saveSlot = saveData.SaveData.SaveData.SaveSlots[slotIndex];

                if (saveSlot.SaveName.IsNullOrEmpty())
                    continue;

                IReadOnlyList<GameProgressionDataItem> dataItems = Rayman1Progression.CreateProgressionItems(
                    saveSlot, out int collectiblesCount, out int maxCollectiblesCount);

                int index = slotIndex;
                yield return new SerializableGameProgressionSlot<BakesaleSaveState<Rayman30thJaguarSave>>(
                    name: saveSlot.SaveName.ToUpper(),
                    index: -1,
                    collectiblesCount: collectiblesCount,
                    totalCollectiblesCount: maxCollectiblesCount,
                    dataItems: dataItems,
                    context: context,
                    serializable: saveData,
                    fileName: fileName)
                {
                    GetExportObject = x => x.SaveData.SaveData.SaveSlots[index],
                    SetImportObject = (x, o) => x.SaveData.SaveData.SaveSlots[index] = (JAG_SaveSlot)o,
                    ExportedType = typeof(JAG_SaveSlot),
                };
            }
        }
    }
}