using System.IO;
using RayCarrot.RCP.Metro.Games.Components;
using RayCarrot.RCP.Metro.ModLoader.Resource;

namespace RayCarrot.RCP.Metro.ModLoader.Modules.Rayman30thMsDosMusic;

public class Rayman30thMsDosMusicModule : ModModule
{
    public override string Id => "30th-dos-music";
    public override LocalizedString Description => new ResourceLocString(nameof(Resources.ModLoader_Rayman30thMsDosMusicModule_Description));

    public Rayman30thMsDosMusicModuleGame[] Games { get; } =
    [
        new("rayman", 0x15FBB0),
        new("raykit", 0x145801),
        new("rayfan", 0x456F5),
        new("ray60", 0x144716),
    ];

    public override IReadOnlyCollection<IModFileResource> GetAddedFiles(Mod mod, FileSystemPath modulePath)
    {
        List<IModFileResource> files = [];

        // Check each MS-DOS game
        foreach (Rayman30thMsDosMusicModuleGame game in Games)
        {
            FileSystemPath dir = modulePath + game.Name;

            if (!dir.DirectoryExists)
                continue;

            // Add the tracks
            foreach (string trackFilePath in Directory.GetFiles(dir, "*.mp3"))
            {
                string fileName = Path.GetFileName(trackFilePath);
                files.Add(new PhysicalModFileResource(
                    path: new ModFilePath($@"roms\DOS\dreamm.ifs\install\rayman\{game.Name}\~mp3music\{fileName}", "assets.pie", BakesaleArchiveComponent.Id),
                    filePath: trackFilePath));
            }
        }

        return files.AsReadOnly();
    }

    public override IReadOnlyCollection<IFilePatch> GetPatchedFiles(Mod mod, FileSystemPath modulePath)
    {
        List<IFilePatch> patches = [];

        // Check each MS-DOS game
        foreach (Rayman30thMsDosMusicModuleGame game in Games)
        {
            FileSystemPath dir = modulePath + game.Name;

            if (!dir.DirectoryExists)
                continue;

            // Get the tracks
            List<Rayman30thMsDosMusicModuleTrack> tracks = [];
            foreach (string trackFilePath in Directory.GetFiles(dir, "*.mp3"))
            {
                const string prefix = "track";
                string fileName = Path.GetFileNameWithoutExtension(trackFilePath);
                if (fileName.Length == prefix.Length + 2 &&
                    fileName.StartsWith(prefix) &&
                    Int32.TryParse(fileName[prefix.Length..], out int track))
                {
                    tracks.Add(new Rayman30thMsDosMusicModuleTrack(trackFilePath, track));
                }
            }

            // Add a patch to the .boot file if any tracks were found
            if (tracks.Any())
            {
                patches.Add(new Rayman30thMsDosMusicFilePatch(
                    path: new ModFilePath($@"save_states\{game.Name}.boot", "assets.pie", BakesaleArchiveComponent.Id),
                    game: game,
                    tracks: tracks));
            }
        }

        return patches.AsReadOnly();
    }
}