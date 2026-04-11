using System.IO;
using BinarySerializer;
using BinarySerializer.Audio.RIFF;
using BinarySerializer.Bakesale;
using ImageMagick;
using K4os.Compression.LZ4;
using RayCarrot.RCP.Metro.ModLoader.Modules.BakesaleResource;
using SharpCompress.Compressors;
using SharpCompress.Compressors.Deflate;

namespace RayCarrot.RCP.Metro.ModLoader.Modules.Deltas;

public class BakesaleResourcePatch : IFilePatch
{
    public BakesaleResourcePatch(ModFilePath path, IReadOnlyList<BakesaleResourceFile> resourceFilePaths)
    {
        Path = path;
        ResourceFilePaths = resourceFilePaths;
    }

    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    public ModFilePath Path { get; }
    public IReadOnlyList<BakesaleResourceFile> ResourceFilePaths { get; }
    
    public void PatchFile(Stream stream)
    {
        // Create the context
        using Context context = new RCPContext(String.Empty);
        RIFFSettings riffSettings = new();
        riffSettings.RegisterSprites();
        riffSettings.RegisterWaves();
        context.AddSettings(riffSettings);

        // Read the resource file
        RIFF_Chunk riff = context.ReadStreamData<RIFF_Chunk>(stream, name: Path.FilePath, mode: VirtualFileMode.DoNotClose);

        if (riff.Data is RIFF_Chunk_RIFF { Type: "SPRT" } sprites)
        {
            // Get the chunks
            RIFF_Chunk_Sprites sprs = sprites.GetRequiredChunk<RIFF_Chunk_Sprites>();
            RIFF_Chunk_List list = sprites.GetRequiredChunk<RIFF_Chunk_List>();

            // Read the sprite sheets
            List<(RIFF_Chunk_ImgFormat Fmt, RIFF_Chunk_Data Data, byte[] ImgData)> spriteSheets = new();
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

                    // Store the data
                    spriteSheets.Add((fmt, data, imgData));
                }
            }

            // Replace the sprites
            foreach (BakesaleResourceFile resourceFile in ResourceFilePaths)
            {
                uint nameHash = resourceFile.GetResourceNameHash();

                int hashTableIndex = Array.IndexOf(sprs.SpriteNameHashes, nameHash);
                if (hashTableIndex == -1)
                {
                    Logger.Warn("Could not find sprite with name hash {0:X8} for resource file {1}", nameHash, resourceFile.FilePathInResource);
                    continue;
                }

                int spriteIndex = sprs.NameHashIndexToSpriteIndexTable[hashTableIndex];
                Sprite sprite = sprs.Sprites[spriteIndex];

                using MagickImage sourceImage = new(resourceFile.SourceFilePath);
                
                // Validate the size
                if (sprite.Width != sourceImage.Width || sprite.Height != sourceImage.Height)
                {
                    Logger.Warn("Sprite dimensions do not match for resource file {0}. Expected: {1}x{2}, Actual: {3}x{4}", resourceFile.FilePathInResource, sprite.Width, sprite.Height, sourceImage.Width, sourceImage.Height);
                    continue;
                }

                byte[] spriteSheetData = spriteSheets[sprite.ImageIndex].ImgData;
                int spriteSheetWidth = spriteSheets[sprite.ImageIndex].Fmt.Width;
                byte[] sourceImageData = sourceImage.ToByteArray(MagickFormat.Rgba);

                for (int y = 0; y < sprite.Height; y++)
                {
                    for (int x = 0; x < sprite.Width; x++)
                    {
                        int spriteSheetIndex = ((sprite.YPosition + y) * spriteSheetWidth + (sprite.XPosition + x)) * 4;
                        int sourceImageIndex = (y * sprite.Width + x) * 4;

                        spriteSheetData[spriteSheetIndex + 0] = sourceImageData[sourceImageIndex + 0];
                        spriteSheetData[spriteSheetIndex + 1] = sourceImageData[sourceImageIndex + 1];
                        spriteSheetData[spriteSheetIndex + 2] = sourceImageData[sourceImageIndex + 2];
                        spriteSheetData[spriteSheetIndex + 3] = sourceImageData[sourceImageIndex + 3];
                    }
                }
            }

            // Compress and replace the sprite sheets
            foreach ((RIFF_Chunk_ImgFormat Fmt, RIFF_Chunk_Data Data, byte[] ImgData) sheet in spriteSheets)
            {
                byte[] compressedData = new byte[LZ4Codec.MaximumOutputSize(sheet.ImgData.Length)];
                int compressedSize = LZ4Codec.Encode(sheet.ImgData, compressedData);
                Array.Resize(ref compressedData, compressedSize);
                sheet.Data.Data = compressedData;
            }
        }
        else if (riff.Data is RIFF_Chunk_RIFF { Type: "WBNK" } waveBank)
        {
            // Get the chunks
            RIFF_Chunk_Waves wavs = waveBank.GetRequiredChunk<RIFF_Chunk_Waves>();
            RIFF_Chunk_List list = waveBank.GetRequiredChunk<RIFF_Chunk_List>();

            // Replace the wave files
            foreach (BakesaleResourceFile resourceFile in ResourceFilePaths)
            {
                uint nameHash = resourceFile.GetResourceNameHash();

                int hashTableIndex = Array.IndexOf(wavs.WaveNameHashes, nameHash);
                if (hashTableIndex == -1)
                {
                    Logger.Warn("Could not find wave with name hash {0:X8} for resource file {1}", nameHash, resourceFile.FilePathInResource);
                    continue;
                }

                int waveIndex = wavs.NameHashIndexToWaveIndexTable[hashTableIndex];

                // Compress and replace the data
                using FileStream sourceFileStream = File.OpenRead(resourceFile.SourceFilePath);
                using MemoryStream compressedMemoryStream = new();
                using ZlibStream zlibStream = new(sourceFileStream, CompressionMode.Compress);
                zlibStream.CopyTo(compressedMemoryStream);
                ((RIFF_Chunk_WaveData)list.Chunks[waveIndex].Data).Data = compressedMemoryStream.ToArray();
            }
        }
        else
        {
            Logger.Warn("Unexpected RIFF type in resource file {0}", Path.FilePath);
            return;
        }

        context.WriteStreamData(stream, riff, name: Path.FilePath, mode: VirtualFileMode.DoNotClose);
        stream.TrimEnd();
    }
}