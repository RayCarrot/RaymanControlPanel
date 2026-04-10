using System.IO;
using System.IO.Compression;
using BinarySerializer;
using BinarySerializer.Audio.RIFF;
using MahApps.Metro.IconPacks;
using SharpCompress.Compressors.Deflate;
using CompressionLevel = System.IO.Compression.CompressionLevel;
using CompressionMode = SharpCompress.Compressors.CompressionMode;

namespace RayCarrot.RCP.Metro.Archive.Bakesale;

public sealed partial class BakesaleWavesFileType : FileType
{
    #region Constructor

    public BakesaleWavesFileType()
    {
        SubFileType = new SubFileType(
            importFormats: new FileExtension[]
            {
                // TODO: Allow importing
            },
            exportFormats: new FileExtension[]
            {
                new(".zip")
            });
    }

    #endregion

    #region Private Properties

    private SubFileType SubFileType { get; }
    private StringCache? StringCache { get; set; }

    #endregion

    #region Public Properties

    public override string TypeDisplayName => "Sounds Resource"; // TODO-LOC
    public override PackIconMaterialKind Icon => PackIconMaterialKind.FileMusicOutline;

    #endregion

    #region Interface Implementations

    public override bool IsSupported(IArchiveDataManager manager) => manager is BakesalePieArchiveDataManager;

    public override bool IsOfType(FileExtension fileExtension, IArchiveDataManager manager) => fileExtension.PrimaryFileExtension == ".waves";

    public override SubFileType GetSubType(FileExtension fileExtension, ArchiveFileStream inputStream, IArchiveDataManager manager) => SubFileType;

    public override void ConvertTo(
        FileExtension inputFormat, 
        FileExtension outputFormat, 
        ArchiveFileStream inputStream, 
        Stream outputStream, 
        IArchiveDataManager manager)
    {
        // Create the string cache if needed
        if (StringCache == null)
        {
            StringCache = new StringCache();
            StringCache.Add(Strings);
        }

        // Create a zip file to write the sounds to
        using ZipArchive zip = new(outputStream, ZipArchiveMode.Create, leaveOpen: true);

        // Create the context
        using Context context = new RCPContext(String.Empty);
        RIFFSettings riffSettings = new();
        riffSettings.RegisterChunkResolver("wavs", (s, data, chunkSize, name) =>
            s.SerializeObject<RIFF_Chunk_Waves>((RIFF_Chunk_Waves)data, x => x.Pre_ChunkSize = chunkSize, name: name));
        riffSettings.RegisterChunkResolver("wdta", (s, data, chunkSize, name) =>
            s.SerializeObject<RIFF_Chunk_WaveData>((RIFF_Chunk_WaveData)data, x => x.Pre_ChunkSize = chunkSize, name: name));
        context.AddSettings(riffSettings);

        // Read the resource file
        RIFF_Chunk riff = context.ReadStreamData<RIFF_Chunk>(inputStream.Stream, name: inputStream.Name, mode: VirtualFileMode.DoNotClose);

        if (riff.Data is not RIFF_Chunk_RIFF { Type: "WBNK" } waveBank)
            throw new Exception("Invalid sound data");

        // Get the chunks
        RIFF_Chunk_Waves wavs = waveBank.GetRequiredChunk<RIFF_Chunk_Waves>();
        RIFF_Chunk_List list = waveBank.GetRequiredChunk<RIFF_Chunk_List>();

        // Get the wave name hashes
        Dictionary<int, uint> waveHashes = new();
        for (int i = 0; i < wavs.WaveNameHashes.Length; i++)
        {
            uint hash = wavs.WaveNameHashes[i];
            if (hash != 0)
                waveHashes.Add(wavs.NameHashIndexToWaveIndexTable[i], hash);
        }

        // Export the waves
        for (int i = 0; i < list.Chunks.Length; i++)
        {
            RIFF_Chunk chunk = list.Chunks[i];
            if (chunk.Data is RIFF_Chunk_WaveData waveData)
            {
                // Decompress the wave data
                using ZlibStream zlibStream = new(new MemoryStream(waveData.Data), CompressionMode.Decompress);

                // Get the path
                string waveOutputPath;
                uint hash = waveHashes[i];
                if (StringCache.TryGetValue(hash, out string? name))
                    waveOutputPath = $"{name}.wav";
                else
                    waveOutputPath = $"_unnamed/{hash:X8}.wav";

                // Export the wave
                ZipArchiveEntry zipEntry = zip.CreateEntry(waveOutputPath, CompressionLevel.Fastest);
                using Stream zipEntryStream = zipEntry.Open();
                zlibStream.CopyTo(zipEntryStream);
            }
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
        throw new NotSupportedException("Importing .waves files is not supported");
    }

    #endregion
}