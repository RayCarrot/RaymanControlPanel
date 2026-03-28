using System.IO;
using BinarySerializer;
using BinarySerializer.UbiArt;

namespace RayCarrot.RCP.Metro.ModLoader.Modules.UbiArtLocalization;

public class UbiArtLocalizationFilePatch<UAString> : IFilePatch
    where UAString : UbiArtString, new()
{
    public UbiArtLocalizationFilePatch(
        GameInstallation gameInstallation, 
        ModFilePath path, 
        IReadOnlyCollection<LocaleFile> localeFiles, 
        FileSystemPath? audioFile)
    {
        GameInstallation = gameInstallation;
        Path = path;
        LocaleFiles = localeFiles;
        AudioFile = audioFile;
    }

    public GameInstallation GameInstallation { get; }
    public ModFilePath Path { get; }
    public IReadOnlyCollection<LocaleFile> LocaleFiles { get; }
    public FileSystemPath? AudioFile { get; }

    public void PatchFile(Stream stream)
    {
        using Context context = new RCPContext(String.Empty);
        context.Initialize(GameInstallation);

        Localisation_Template<UAString> loc = context.ReadStreamData<Localisation_Template<UAString>>(stream, name: Path.FilePath, endian: Endian.Big, mode: VirtualFileMode.DoNotClose);

        foreach (LocaleFile localeFile in LocaleFiles)
        {
            List<UbiArtKeyObjValuePair<int, UAString>>? stringTable = loc.Strings.FirstOrDefault(x => x.Key == localeFile.Id)?.Value.ToList();

            if (stringTable == null)
                continue;

            string[] lines = File.ReadAllLines(localeFile.FilePath);

            foreach (string line in lines)
            {
                if (line.IsNullOrWhiteSpace())
                    continue;

                int separatorIndex = line.IndexOf('=');

                if (separatorIndex == -1)
                    continue;

                string locIdString = line.Substring(0, separatorIndex);
                if (!Int32.TryParse(locIdString, out int locId))
                    continue;

                string locValue = line.Substring(separatorIndex + 1);

                // Replace \n with a linebreak
                locValue = locValue.Replace(@"\n", "\n");

                if (stringTable.FirstOrDefault(x => x.Key == locId) is { } pair)
                {
                    pair.Value = new UAString { Value = locValue };
                }
                else
                {
                    stringTable.Add(new UbiArtKeyObjValuePair<int, UAString>()
                    {
                        Key = locId,
                        Value = new UAString { Value = locValue },
                    });
                }
            }

            loc.Strings.First(x => x.Key == localeFile.Id).Value = stringTable.ToArray();
        }

        if (AudioFile != null)
        {
            List<UbiArtKeyObjValuePair<int, LocAudio<UAString>>> audioTable = loc.Audio.ToList();

            string[] lines = File.ReadAllLines(AudioFile);

            foreach (string line in lines)
            {
                if (line.IsNullOrWhiteSpace())
                    continue;

                int separatorIndex = line.IndexOf('=');

                if (separatorIndex == -1)
                    continue;

                string locIdString = line.Substring(0, separatorIndex);
                if (!Int32.TryParse(locIdString, out int locId))
                    continue;

                string audioPath = line.Substring(separatorIndex + 1);

                // Default to -10 since that's the most common value the game uses
                float audioVolume = -10;

                // Attempt to get the volume from the string
                int volumeSeparatorIndex = audioPath.IndexOf('|');
                if (volumeSeparatorIndex != -1)
                {
                    string volumeString = audioPath.Substring(volumeSeparatorIndex + 1);
                    if (Single.TryParse(volumeString, out float parsedVolume))
                        audioVolume = parsedVolume;

                    audioPath = audioPath.Substring(0, volumeSeparatorIndex);
                }

                LocAudio<UAString> locAudio = new()
                {
                    LocalisationId = 0, // Always 0 in file
                    AudioFile = new UAString() { Value = audioPath },
                    AudioVolume = audioVolume
                };

                if (audioTable.FirstOrDefault(x => x.Key == locId) is { } pair)
                {
                    pair.Value = locAudio;
                }
                else
                {
                    audioTable.Add(new UbiArtKeyObjValuePair<int, LocAudio<UAString>>()
                    {
                        Key = locId,
                        Value = locAudio,
                    });
                }
            }

            loc.Audio = audioTable.ToArray();
        }

        context.WriteStreamData(stream, loc, name: Path.FilePath, endian: Endian.Big, mode: VirtualFileMode.DoNotClose);
        stream.TrimEnd();
    }
}