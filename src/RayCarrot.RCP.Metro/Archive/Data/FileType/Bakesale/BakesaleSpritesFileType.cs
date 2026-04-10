using System.IO;
using System.IO.Compression;
using BinarySerializer;
using BinarySerializer.Audio.RIFF;
using ImageMagick;
using K4os.Compression.LZ4;
using MahApps.Metro.IconPacks;

namespace RayCarrot.RCP.Metro.Archive.Bakesale;

public sealed partial class BakesaleSpritesFileType : FileType
{
    #region Constructor

    public BakesaleSpritesFileType()
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

    public override string TypeDisplayName => "Sprites Resource"; // TODO-LOC
    public override PackIconMaterialKind Icon => PackIconMaterialKind.FileImageOutline;

    #endregion

    #region Interface Implementations

    public override bool IsSupported(IArchiveDataManager manager) => manager is BakesalePieArchiveDataManager;

    public override bool IsOfType(FileExtension fileExtension, IArchiveDataManager manager) => fileExtension.PrimaryFileExtension == ".sprite";

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

        // Create a zip file to write the sprites to
        using ZipArchive zip = new(outputStream, ZipArchiveMode.Create, leaveOpen: true);

        // Create the context
        using Context context = new RCPContext(String.Empty);
        RIFFSettings riffSettings = new();
        riffSettings.RegisterChunkResolver("sprs", (s, data, chunkSize, name) =>
            s.SerializeObject<RIFF_Chunk_Sprites>((RIFF_Chunk_Sprites)data, x => x.Pre_ChunkSize = chunkSize, name: name));
        riffSettings.RegisterChunkResolver("fmt ", (s, data, chunkSize, name) =>
            s.SerializeObject<RIFF_Chunk_ImgFormat>((RIFF_Chunk_ImgFormat)data, x => x.Pre_ChunkSize = chunkSize, name: name));
        context.AddSettings(riffSettings);

        // Read the resource file
        RIFF_Chunk riff = context.ReadStreamData<RIFF_Chunk>(inputStream.Stream, name: inputStream.Name, mode: VirtualFileMode.DoNotClose);

        if (riff.Data is not RIFF_Chunk_RIFF { Type: "SPRT" } sprites)
            throw new Exception("Invalid sprite data");

        // Get the chunks
        RIFF_Chunk_Sprites sprs = sprites.GetRequiredChunk<RIFF_Chunk_Sprites>();
        RIFF_Chunk_List list = sprites.GetRequiredChunk<RIFF_Chunk_List>();

        List<MagickImage> images = [];
        try
        {
            // Export the sprite-sheets
            foreach (RIFF_Chunk chunk in list.Chunks)
            {
                if (chunk.Data is RIFF_Chunk_RIFF { Type: "IMG " } img)
                {
                    // Get the chunks
                    RIFF_Chunk_ImgFormat fmt = img.GetRequiredChunk<RIFF_Chunk_ImgFormat>();
                    RIFF_Chunk_Data data = img.GetRequiredChunk<RIFF_Chunk_Data>();

                    // Decompress the image data
                    byte[] imgData = new byte[fmt.Width * fmt.Height * 4];
                    LZ4Codec.Decode(data.Data, imgData);

                    // Create an image from the data
                    MagickImage image = new(imgData, new MagickReadSettings()
                    {
                        Width = fmt.Width,
                        Height = fmt.Height,
                        Format = MagickFormat.Rgba,
                    });
                    image.Strip();

                    // Save the sheet
                    images.Add(image);

                    // Export the sheet
                    ZipArchiveEntry zipEntry = zip.CreateEntry($"_sheets/{fmt.Name}.png", CompressionLevel.Fastest);
                    using Stream zipEntryStream = zipEntry.Open();
                    image.Write(zipEntryStream);
                }
            }

            // Get the sprite name hashes
            Dictionary<int, uint> spriteHashes = new();
            for (int i = 0; i < sprs.SpriteNameHashes.Length; i++)
            {
                uint hash = sprs.SpriteNameHashes[i];
                if (hash != 0)
                    spriteHashes.Add(sprs.NameHashIndexToSpriteIndexTable[i], hash);
            }

            // Export the sprites
            for (int i = 0; i < sprs.Sprites.Length; i++)
            {
                // Get the sprite
                Sprite sprite = sprs.Sprites[i];

                // Clone the sprite-sheet image and crop it to the sprite
                using IMagickImage<byte> image = images[sprite.ImageIndex].Clone();
                image.Crop(new MagickGeometry(sprite.XPosition, sprite.YPosition, (uint)sprite.Width, (uint)sprite.Height));
                image.Strip();

                // Get the path
                string spriteOutputPath;
                uint hash = spriteHashes[i];
                if (StringCache.TryGetValue(hash, out string? name))
                    spriteOutputPath = $"{name}.png";
                else
                    spriteOutputPath = $"_unnamed/{hash:X8}.png";

                // Export the sprite
                ZipArchiveEntry zipEntry = zip.CreateEntry(spriteOutputPath, CompressionLevel.Fastest);
                using Stream zipEntryStream = zipEntry.Open();
                image.Write(zipEntryStream);
            }
        }
        finally
        {
            foreach (MagickImage img in images)
                img.Dispose();
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
        throw new NotSupportedException("Importing .sprite files is not supported");
    }

    #endregion
}