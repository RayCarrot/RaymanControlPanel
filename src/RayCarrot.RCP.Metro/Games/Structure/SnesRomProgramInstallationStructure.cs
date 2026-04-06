namespace RayCarrot.RCP.Metro.Games.Structure;

public class SnesRomProgramInstallationStructure : RomProgramInstallationStructure
{
    public override bool SupportGameFileFinder => false;
    public override FileExtension[] SupportedFileExtensions => new[]
    {
        new FileExtension(".sfc"),
        new FileExtension(".smc"),
    };
}