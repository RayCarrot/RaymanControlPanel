using System.IO;
using BinarySerializer;
using BinarySerializer.PlayStation.PS3;

namespace RayCarrot.RCP.Metro.Imaging;

public class GtfImageFormat : ImageFormat
{
    public override string Id => "GTF";
    public override string Name => "GTF";

    public override bool CanDecode => true;
    public override bool CanEncode => false;

    public override FileExtension[] FileExtensions { get; } =
    {
        new(".gtf"),
    };

    private ImageMetadata GetMetadata(GTFTexture tex)
    {
        return new ImageMetadata(tex.Width, tex.Height);
    }

    public override ImageMetadata GetMetadata(Stream inputStream)
    {
        // Read the file
        using Context context = new RCPContext(String.Empty);
        GTF gtf = context.ReadStreamData<GTF>(inputStream, endian: Endian.Big, mode: VirtualFileMode.DoNotClose, maintainPosition: true);

        return GetMetadata(gtf.Textures[0]);
    }

    public override RawImageData Decode(Stream inputStream)
    {
        // Read the file
        using Context context = new RCPContext(String.Empty);
        GTF gtf = context.ReadStreamData<GTF>(inputStream, endian: Endian.Big, mode: VirtualFileMode.DoNotClose, maintainPosition: true);

        if (gtf.TexturesCount != 1)
            throw new InvalidOperationException("GTF files with more than 1 texture are not supported");

        GTFTexture texture = gtf.Textures[0];

        if (texture.Depth != 1 || texture.Cubemap || texture.Dimension != GTFDimension.Dimension2)
            throw new InvalidOperationException("Only 2D GTF textures are supported");

        Func<DuoGridItemViewModel[]> customInfoItemsFactory = () =>
        [
            new DuoGridItemViewModel(
                header: new ResourceLocString(nameof(Resources.Archive_FileInfo_Img_Encoding)),
                text: texture.Format.ToString(),
                minUserLevel: UserLevel.Technical)
        ];

        MipmapImage[] mipmaps = new MipmapImage[texture.MipmapLevels];
        int imgOffset = 0;
        int mipmapWidth = texture.Width;
        int mipmapHeight = texture.Height;
        for (int i = 0; i < texture.MipmapLevels; i++)
        {
            int mipmapImgLength = texture.Format switch
            {
                GTFFormat.A8R8G8B8 => mipmapWidth * mipmapHeight * 4,
                GTFFormat.COMPRESSED_DXT1 => BlockCompressionHelpers.GetImageLength(RawImageDataCompressedFormat.DXT1, mipmapWidth, mipmapHeight),
                GTFFormat.COMPRESSED_DXT23 => BlockCompressionHelpers.GetImageLength(RawImageDataCompressedFormat.DXT3, mipmapWidth, mipmapHeight),
                GTFFormat.COMPRESSED_DXT45 => BlockCompressionHelpers.GetImageLength(RawImageDataCompressedFormat.DXT5, mipmapWidth, mipmapHeight),
                _ => throw new InvalidOperationException($"The GTF format {texture.Format} is not supported"),
            };

            byte[] mipmapImgData = new byte[mipmapImgLength];
            Array.Copy(texture.TextureData, imgOffset, mipmapImgData, 0, mipmapImgLength);

            // Unswizzle
            if (texture.Format == GTFFormat.A8R8G8B8)
            {
                MortonSwizzle swizzle = new(mipmapWidth, mipmapHeight, 4);
                mipmapImgData = swizzle.Unswizzle(mipmapImgData);
            }

            mipmaps[i] = new MipmapImage(mipmapImgData, mipmapWidth, mipmapHeight);

            imgOffset += mipmapImgLength;
            mipmapWidth = Math.Max(1, mipmapWidth >> 1);
            mipmapHeight = Math.Max(1, mipmapHeight >> 1);
        }

        switch (texture.Format)
        {
            case GTFFormat.A8R8G8B8:
                // Convert ARGB to BGRA
                foreach (MipmapImage mipmapImage in mipmaps)
                {
                    for (int i = 0; i < mipmapImage.ImageData.Length; i += 4)
                    {
                        byte a = mipmapImage.ImageData[i + 0];
                        byte r = mipmapImage.ImageData[i + 1];
                        byte g = mipmapImage.ImageData[i + 2];
                        byte b = mipmapImage.ImageData[i + 3];

                        mipmapImage.ImageData[i + 0] = b;
                        mipmapImage.ImageData[i + 1] = g;
                        mipmapImage.ImageData[i + 2] = r;
                        mipmapImage.ImageData[i + 3] = a;
                    }
                }

                return new RawImageData(mipmaps, RawImageDataPixelFormat.Bgra32)
                {
                    CustomInfoItemsFactory = customInfoItemsFactory
                };

            case GTFFormat.COMPRESSED_DXT1:
                return new RawImageData(mipmaps, RawImageDataCompressedFormat.DXT1)
                {
                    CustomInfoItemsFactory = customInfoItemsFactory
                };

            case GTFFormat.COMPRESSED_DXT23:
                return new RawImageData(mipmaps, RawImageDataCompressedFormat.DXT3)
                {
                    CustomInfoItemsFactory = customInfoItemsFactory
                };

            case GTFFormat.COMPRESSED_DXT45:
                return new RawImageData(mipmaps, RawImageDataCompressedFormat.DXT5)
                {
                    CustomInfoItemsFactory = customInfoItemsFactory
                };

            default:
                throw new InvalidOperationException($"The GTF format {texture.Format} is not supported");
        }
    }

    public override ImageMetadata Encode(RawImageData data, Stream outputStream)
    {
        throw new InvalidOperationException();
    }
}