namespace RayCarrot.RCP.Metro.ModLoader.Modules.Rayman30thMsDosMusic;

public struct Rayman30thMsDosMusicModuleGame
{
    public Rayman30thMsDosMusicModuleGame(string name, long bootOffset)
    {
        Name = name;
        BootOffset = bootOffset;
    }

    public string Name { get; }
    public long BootOffset { get; }
}