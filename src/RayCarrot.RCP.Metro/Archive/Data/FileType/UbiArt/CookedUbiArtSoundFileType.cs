using System.IO;
using System.Text;
using BinarySerializer;
using BinarySerializer.Audio;
using BinarySerializer.Audio.RIFF;
using BinarySerializer.UbiArt;
using MahApps.Metro.IconPacks;

namespace RayCarrot.RCP.Metro.Archive.UbiArt;

/// <summary>
/// A cooked sound .wav file type
/// </summary>
public sealed class CookedUbiArtSoundFileType : FileType
{
    #region Constructor

    public CookedUbiArtSoundFileType()
    {
        SubFileType = new SubFileType(
            importFormats: new FileExtension[]
            {
                new(".wav")
            },
            exportFormats: new FileExtension[]
            {
                new(".wav")
            });
    }

    #endregion

    #region Private Properties

    private SubFileType SubFileType { get; }

    #endregion

    #region Public Properties

    public override string TypeDisplayName => Resources.Archive_Format_CookedUbiArtSound;
    public override PackIconMaterialKind Icon => PackIconMaterialKind.FileMusicOutline;

    #endregion

    #region Interface Implementations

    public override bool IsSupported(IArchiveDataManager manager) => manager.Context?.HasSettings<UbiArtSettings>() is true;

    public override bool IsOfType(FileExtension fileExtension, ArchiveFileStream inputStream, IArchiveDataManager manager)
    {
        if (fileExtension != new FileExtension(".wav.ckd", multiple: true))
            return false;

        // Get the settings
        UbiArtSettings settings = manager.Context!.GetRequiredSettings<UbiArtSettings>();

        // TODO: Support other games and platforms
        if (settings is { Game: BinarySerializer.UbiArt.Game.RaymanOrigins, Platform: Platform.PC })
        {
            inputStream.SeekToBeginning();
            using Reader reader = new(inputStream.Stream, true, true);
            string identifier = reader.ReadString(4, Encoding.ASCII);
            reader.ReadUInt32(); // Skip size
            string type = reader.ReadString(4, Encoding.ASCII);

            return identifier is "RIFF" && type is "WAVE";
        }
        else if (settings is { Game: BinarySerializer.UbiArt.Game.RaymanLegends, Platform: Platform.PC or Platform.PlayStation4 })
        {
            inputStream.SeekToBeginning();
            using Reader reader = new(inputStream.Stream, settings.Endian == Endian.Little, true);
            uint version = reader.ReadUInt32();
            string magic = reader.ReadString(4, Encoding.UTF8);

            if (magic != "RAKI")
                return false;

            reader.ReadUInt32(); // Skip compress value
            string platform = reader.ReadString(4, Encoding.UTF8);
            string format = reader.ReadString(4, Encoding.UTF8);

            // Rayman Legends (PC)
            if (version is 11 &&
                platform is "Win " &&
                format is "pcm " or "adpc")
                return true;
            // Rayman Legends (PS4)
            else if (version is 15 &&
                     platform is "Orbi" &&
                     format is "pcm ")
                return true;
            else
                return false;
        }
        else
        {
            return false;
        }
    }

    public override SubFileType GetSubType(FileExtension fileExtension, ArchiveFileStream inputStream, IArchiveDataManager manager) => SubFileType;

    public override void ConvertTo(
        FileExtension inputFormat, 
        FileExtension outputFormat, 
        ArchiveFileStream inputStream, 
        Stream outputStream, 
        IArchiveDataManager manager)
    {
        // Get the settings
        UbiArtSettings settings = manager.Context!.GetRequiredSettings<UbiArtSettings>();

        if (settings is { Game: BinarySerializer.UbiArt.Game.RaymanOrigins, Platform: Platform.PC })
        {
            // Same format, but different file extensions. Just copy the data.
            inputStream.Stream.CopyToEx(outputStream);
        }
        else if (settings is { Game: BinarySerializer.UbiArt.Game.RaymanLegends, Platform: Platform.PC or Platform.PlayStation4 })
        {
            // Read the RAKI data
            Raki raki = manager.Context!.ReadStreamData<Raki>(inputStream.Stream, name: inputStream.Name, mode: VirtualFileMode.DoNotClose);

            // Convert to WAVE
            RIFF_Chunk_RIFF riff = new()
            {
                Type = "WAVE",
                Chunks = new RIFF_Chunk[raki.ChunkHeaders.Length]
            };

            // Convert chunks
            for (int i = 0; i < raki.ChunkHeaders.Length; i++)
            {
                RakiChunkHeader header = raki.ChunkHeaders[i];
                RakiChunk chunk = raki.Chunks[i];

                riff.Chunks[i] = new RIFF_Chunk
                {
                    Identifier = header.Identifier,
                    Data = chunk.Data,
                };
            }

            RIFF_Chunk wavFile = riff.CreateChunk();

            // Write the .wav file using a new context since we don't want the UbiArt settings here
            using Context context = new RCPContext(String.Empty);
            RIFFSettings riffSettings = new();
            riffSettings.RegisterWAV();
            context.AddSettings(riffSettings);
            context.WriteStreamData(outputStream, wavFile, mode: VirtualFileMode.DoNotClose);
        }
    }

    public override void ConvertFrom(
        FileExtension inputFormat, 
        FileExtension outputFormat, 
        ArchiveFileStream currentFileStream, 
        ArchiveFileStream inputStream, 
        ArchiveFileStream outputStream, 
        IArchiveDataManager manager)
    {
        // Get the settings
        UbiArtSettings settings = manager.Context!.GetRequiredSettings<UbiArtSettings>();

        if (settings is { Game: BinarySerializer.UbiArt.Game.RaymanOrigins, Platform: Platform.PC })
        {
            // Same format, but different file extensions. Just copy the data.
            inputStream.Stream.CopyToEx(outputStream.Stream);
        }
        else if (settings is { Game: BinarySerializer.UbiArt.Game.RaymanLegends, Platform: Platform.PC or Platform.PlayStation4 })
        {
            // Read the .wav file using a new context since we don't want the UbiArt settings here
            WAV wavFile;
            using (Context context = new RCPContext(String.Empty))
            {
                RIFFSettings riffSettings = new();
                riffSettings.RegisterWAV();
                context.AddSettings(riffSettings);
                wavFile = context.ReadStreamData<WAV>(inputStream.Stream, name: inputStream.Name, mode: VirtualFileMode.DoNotClose);
            }

            // Get the riff chunks
            RIFF_Chunk_Format formatChunk = wavFile.Format;
            RIFF_Chunk_Data dataChunk = wavFile.Data;

            // Only keep format and data chunks to save on memory
            // TODO: Include MARK and STRG as well?
            RIFF_Chunk[] riffChunks =
            {
                formatChunk.CreateChunk(),
                dataChunk.CreateChunk(), // Data chunk should always be last
            };

            // Read the RAKI data
            Raki raki = manager.Context!.ReadStreamData<Raki>(currentFileStream.Stream, name: currentFileStream.Name, mode: VirtualFileMode.DoNotClose);

            // Set the format
            // TODO: Handle other cases, throw error if invalid
            raki.Format = formatChunk.FormatType == 2 ? "adpc" : "pcm ";

            // Convert chunks
            raki.ChunkHeaders = riffChunks.ToArray(x => new RakiChunkHeader
            {
                Identifier = x.Identifier,
                ChunkSize = (int)x.Data.Pre_ChunkSize,
            });
            raki.Chunks = riffChunks.ToArray(x => new RakiChunk
            {
                Data = x.Data
            });

            // Calculate offsets
            raki.CalculateOffsets();

            // Write the RAKI file
            manager.Context!.WriteStreamData(outputStream.Stream, raki, name: outputStream.Name, mode: VirtualFileMode.DoNotClose);
        }
    }

    #endregion
}