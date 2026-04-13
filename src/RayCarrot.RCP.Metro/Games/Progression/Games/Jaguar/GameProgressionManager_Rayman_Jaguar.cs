using BinarySerializer.Ray1.Jaguar;

namespace RayCarrot.RCP.Metro;

public class GameProgressionManager_Rayman_Jaguar : EmulatedGameProgressionManager
{
    public GameProgressionManager_Rayman_Jaguar(GameInstallation gameInstallation, string progressionId) 
        : base(gameInstallation, progressionId) 
    { }

    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    public override async IAsyncEnumerable<EmulatedGameProgressionSlot> LoadSlotsAsync(EmulatedSave emulatedSave)
    {
        JAG_SaveData saveData = await emulatedSave.ReadAsync<JAG_SaveData>();

        for (int saveIndex = 0; saveIndex < saveData.SaveSlots.Length; saveIndex++)
        {
            JAG_SaveSlot saveSlot = saveData.SaveSlots[saveIndex];

            if (saveSlot.SaveName.IsNullOrEmpty())
                continue;

            IReadOnlyList<GameProgressionDataItem> dataItems = Rayman1Progression.CreateProgressionItems(
                saveSlot, out int collectiblesCount, out int maxCollectiblesCount);

            int slotIndex = saveIndex;

            yield return new SerializabeEmulatedGameProgressionSlot<JAG_SaveData>(
                name: saveSlot.SaveName.ToUpper(),
                index: saveIndex,
                collectiblesCount: collectiblesCount,
                totalCollectiblesCount: maxCollectiblesCount,
                emulatedSave: emulatedSave,
                dataItems: dataItems,
                serializable: saveData)
            {
                GetExportObject = x => x.SaveSlots[slotIndex],
                SetImportObject = (x, o) => x.SaveSlots[slotIndex] = (JAG_SaveSlot)o,
                ExportedType = typeof(JAG_SaveSlot)
            };
        }

        Logger.Info("{0} save has been loaded", GameInstallation.FullId);
    }
}