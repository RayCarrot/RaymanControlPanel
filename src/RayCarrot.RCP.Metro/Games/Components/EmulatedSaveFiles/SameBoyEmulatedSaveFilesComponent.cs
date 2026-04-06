namespace RayCarrot.RCP.Metro.Games.Components;

public class SameBoyEmulatedSaveFilesComponent : EmulatedSaveFilesComponent
{
    public SameBoyEmulatedSaveFilesComponent() : base(GetEmulatedSaveFiles) { }

    public static IEnumerable<EmulatedSaveFile> GetEmulatedSaveFiles(GameInstallation gameInstallation)
    {
        FileSystemPath saveFilePath = gameInstallation.InstallLocation.FilePath;
        saveFilePath = saveFilePath.ChangeFileExtension(new FileExtension(".sav"));

        if (saveFilePath.FileExists)
            yield return new EmulatedGbcSaveFile(saveFilePath);
    }
}