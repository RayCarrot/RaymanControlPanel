using System.IO;
using RayCarrot.RCP.Metro.ModLoader.Metadata;
using RayCarrot.RCP.Metro.ModLoader.Modules.Deltas;

namespace RayCarrot.RCP.Metro.ModLoader.Modules.BakesaleResource;

public class BakesaleResourceModule : ModModule
{
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    public const string SpritesResourceFileExtension = ".sprite";
    public const string WavesResourceFileExtension = ".waves";

    public override string Id => "bakesale-resources";
    public override LocalizedString Description => "This allows replacing individual files within sprite and sound resources. Keep in mind that sprites have to maintain the same dimensions."; // TODO-LOC

    public override IReadOnlyCollection<IFilePatch> GetPatchedFiles(Mod mod, FileSystemPath modulePath)
    {
        Dictionary<ModFilePath, List<BakesaleResourceFile>> resourceFiles = new();

        foreach (FileSystemPath file in Directory.EnumerateFiles(modulePath, "*", SearchOption.AllDirectories))
        {
            string relativeFilePath = file.RemoveFileExtension() - modulePath;

            // Get the path of the resource file
            string resourceFilePath;
            int resourceExtIndex = relativeFilePath.IndexOf(SpritesResourceFileExtension, StringComparison.InvariantCulture);
            if (resourceExtIndex == -1)
            {
                resourceExtIndex = relativeFilePath.IndexOf(WavesResourceFileExtension, StringComparison.InvariantCulture);
                if (resourceExtIndex == -1)
                {
                    Logger.Warn("File {0} is not within a valid resource file", relativeFilePath);
                    continue;
                }
                else
                {
                    resourceFilePath = relativeFilePath[..(resourceExtIndex + WavesResourceFileExtension.Length)];
                }
            }
            else
            {
                resourceFilePath = relativeFilePath[..(resourceExtIndex + SpritesResourceFileExtension.Length)];
            }

            relativeFilePath = relativeFilePath[(resourceFilePath.Length + 1)..];

            bool inArchive = false;

            if (mod.Metadata.Archives != null)
            {
                foreach (ModArchiveInfo archive in mod.Metadata.Archives)
                {
                    if (resourceFilePath.StartsWith(archive.FilePath))
                    {
                        ModFilePath modFilePath = new(resourceFilePath[(archive.FilePath.Length + 1)..], archive.FilePath, archive.Id);

                        if (resourceFiles.TryGetValue(modFilePath, out List<BakesaleResourceFile> files))
                            files.Add(new BakesaleResourceFile(relativeFilePath, file));
                        else
                            resourceFiles.Add(modFilePath, [new BakesaleResourceFile(relativeFilePath, file)]);

                        inArchive = true;
                        break;
                    }
                }
            }

            if (!inArchive)
            {
                ModFilePath modFilePath = new(resourceFilePath);
                if (resourceFiles.TryGetValue(modFilePath, out List<BakesaleResourceFile> files))
                    files.Add(new BakesaleResourceFile(relativeFilePath, file));
                else
                    resourceFiles.Add(modFilePath, [new BakesaleResourceFile(relativeFilePath, file)]);
            }
        }

        List<IFilePatch> filePatches = [];
        foreach (KeyValuePair<ModFilePath, List<BakesaleResourceFile>> kvp in resourceFiles)
            filePatches.Add(new BakesaleResourcePatch(kvp.Key, kvp.Value));
        return filePatches.AsReadOnly();
    }
}