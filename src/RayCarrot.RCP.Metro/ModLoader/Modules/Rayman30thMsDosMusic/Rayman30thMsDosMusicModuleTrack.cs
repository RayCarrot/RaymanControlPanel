namespace RayCarrot.RCP.Metro.ModLoader.Modules.Rayman30thMsDosMusic;

public struct Rayman30thMsDosMusicModuleTrack
{
    public Rayman30thMsDosMusicModuleTrack(FileSystemPath filePath, int track)
    {
        FilePath = filePath;
        Track = track;
    }

    public FileSystemPath FilePath { get; }
    public int Track { get; }
}