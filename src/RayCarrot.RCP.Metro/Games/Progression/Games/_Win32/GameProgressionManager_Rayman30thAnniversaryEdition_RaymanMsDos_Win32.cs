using System.IO;
using BinarySerializer.Bakesale;
using BinarySerializer.Ray1;

namespace RayCarrot.RCP.Metro;

public class GameProgressionManager_Rayman30thAnniversaryEdition_RaymanMsDos_Win32 : GameProgressionManager_Rayman30thAnniversaryEdition_Win32
{
    public GameProgressionManager_Rayman30thAnniversaryEdition_RaymanMsDos_Win32(GameDescriptor_Rayman30thAnniversaryEdition_Win32 gameDescriptor, GameInstallation gameInstallation, string progressionId, string gameId) 
        : base(gameDescriptor, gameInstallation, progressionId, gameId) { }

    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    public override string Name => "Rayman - MS-DOS"; // TODO-LOC?

    public override async IAsyncEnumerable<GameProgressionSlot> LoadSlotsAsync(FileSystemWrapper fileSystem)
    {
        // Get the save directory
        FileSystemPath? saveDir = fileSystem.GetDirectory(new IOSearchPattern(GameDescriptor.GetSaveDirectory(GameInstallation), SearchOption.TopDirectoryOnly, $"{GameId}.bsav?"))?.DirPath;

        if (saveDir == null)
            yield break;

        // Create the context
        using RCPContext context = new(saveDir);
        Ray1Settings settings = new(Ray1EngineVersion.PC);
        context.AddSettings(settings);

        // Check every save (the collection save states)
        for (int saveIndex = 0; saveIndex < SavesPerGame; saveIndex++)
        {
            string fileName = $"{GameId}.bsav{saveIndex}";

            Logger.Info("{0} save {1} is being loaded...", GameInstallation.FullId, saveIndex);

            // Load the save
            BakesaleSaveFile<DreammSaveData>? saveData = await context.ReadFileDataAsync<BakesaleSaveFile<DreammSaveData>>(fileName, removeFileWhenComplete: false);

            if (saveData?.SaveData == null)
            {
                Logger.Info("{0} save {1} was not found", GameInstallation.FullId, saveIndex);
                continue;
            }

            Logger.Info("{0} save {1} has been deserialized", GameInstallation.FullId, saveIndex);

            // Load every in-game save slot
            for (int slotIndex = 0; slotIndex < 3; slotIndex++)
            {
                int virtualFileId = 49 + slotIndex;
                SaveSlot? slotData = saveData.SaveData.VirtualSaveFiles.FirstOrDefault(v => v.VirtualFileId == virtualFileId)?.VirtualFile;

                if (slotData == null)
                    continue;

                IReadOnlyList<GameProgressionDataItem> dataItems = Rayman1Progression.CreateProgressionItems(
                    slotData, out int collectiblesCount, out int maxCollectiblesCount);

                yield return new SerializableGameProgressionSlot<BakesaleSaveFile<DreammSaveData>>(
                    name: slotData.SaveName.ToUpper(),
                    index: -1,
                    collectiblesCount: collectiblesCount,
                    totalCollectiblesCount: maxCollectiblesCount,
                    dataItems: dataItems,
                    context: context,
                    serializable: saveData,
                    fileName: fileName,
                    // NOTE: Disable importing for now since the game doesn't load the raw saves, except when resetting, but then it
                    //       seems to write the saves immediately before re-reading them, so we can't intercept that...
                    canImport: false)
                {
                    GetExportObject = x => x.SaveData.VirtualSaveFiles.First(v => v.VirtualFileId == virtualFileId).VirtualFile,
                    SetImportObject = (x, o) => x.SaveData.VirtualSaveFiles.First(v => v.VirtualFileId == virtualFileId).VirtualFile = (SaveSlot)o,
                    ExportedType = typeof(SaveSlot),
                };
            }
        }
    }
}