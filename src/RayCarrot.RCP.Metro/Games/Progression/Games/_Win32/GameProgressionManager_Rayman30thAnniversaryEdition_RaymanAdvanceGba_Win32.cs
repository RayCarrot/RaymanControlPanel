using System.IO;
using BinarySerializer.Bakesale;
using BinarySerializer.Nintendo.GBA;
using BinarySerializer.Ray1;
using BinarySerializer.Ray1.GBA;

namespace RayCarrot.RCP.Metro;

public class GameProgressionManager_Rayman30thAnniversaryEdition_RaymanAdvanceGba_Win32 : GameProgressionManager_Rayman30thAnniversaryEdition_Win32
{
    public GameProgressionManager_Rayman30thAnniversaryEdition_RaymanAdvanceGba_Win32(GameDescriptor_Rayman30thAnniversaryEdition_Win32 gameDescriptor, GameInstallation gameInstallation, string progressionId, string gameId) 
        : base(gameDescriptor, gameInstallation, progressionId, gameId) { }

    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    public override string Name => "Rayman Advance - Game Boy Advance"; // TODO-LOC?

    public override async IAsyncEnumerable<GameProgressionSlot> LoadSlotsAsync(FileSystemWrapper fileSystem)
    {
        // Get the save directory
        FileSystemPath? saveDir = fileSystem.GetDirectory(new IOSearchPattern(GameDescriptor.GetSaveDirectory(GameInstallation), SearchOption.TopDirectoryOnly, $"{GameId}.bsav?"))?.DirPath;

        if (saveDir == null)
            yield break;

        // Create the context
        using RCPContext context = new(saveDir);
        Ray1Settings settings = new(Ray1EngineVersion.GBA);
        context.AddSettings(settings);

        // Check every save (the collection save states)
        for (int saveIndex = 0; saveIndex < SavesPerGame; saveIndex++)
        {
            string fileName = $"{GameId}.bsav{saveIndex}";

            Logger.Info("{0} save {1} is being loaded...", GameInstallation.FullId, saveIndex);

            // Load the save
            BakesaleSaveFile<EEPROM<SaveData>>? saveData = await context.ReadFileDataAsync<BakesaleSaveFile<EEPROM<SaveData>>>(
                fileName: fileName, 
                onPreSerialize: x => x.Pre_OnPreSerializeSaveData = y => y.Pre_Size = EEPROM<SaveData>.EEPROMSize.Kbit_64, 
                removeFileWhenComplete: false);

            if (saveData?.SaveData == null)
            {
                Logger.Info("{0} save {1} was not found", GameInstallation.FullId, saveIndex);
                continue;
            }

            Logger.Info("{0} save {1} has been deserialized", GameInstallation.FullId, saveIndex);

            // Load every in-game save slot
            for (int slotIndex = 0; slotIndex < saveData.SaveData.Obj.SaveSlots.Length; slotIndex++)
            {
                SaveSlot slotData = saveData.SaveData.Obj.SaveSlots[slotIndex];

                if (!slotData.GBA_IsValid)
                    continue;

                IReadOnlyList<GameProgressionDataItem> dataItems = Rayman1Progression.CreateProgressionItems(
                    slotData, out int collectiblesCount, out int maxCollectiblesCount);

                int index = slotIndex;
                yield return new SerializableGameProgressionSlot<BakesaleSaveFile<EEPROM<SaveData>>>(
                    name: slotData.SaveName.ToUpper().Replace('~', '△'),
                    index: -1,
                    collectiblesCount: collectiblesCount,
                    totalCollectiblesCount: maxCollectiblesCount,
                    dataItems: dataItems,
                    context: context,
                    serializable: saveData,
                    fileName: fileName)
                {
                    GetExportObject = x => x.SaveData.Obj.SaveSlots[index],
                    SetImportObject = (x, o) => x.SaveData.Obj.SaveSlots[index] = (SaveSlot)o,
                    ExportedType = typeof(SaveSlot),
                };
            }
        }
    }
}