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
        return new ImageMetadata(tex.Width, tex.Height)
        {
            MipmapsCount = tex.MipmapLevels,
            Encoding = tex.Format.ToString(),
        };
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
        byte[] imgData = texture.TextureData;

        if (texture.Cubemap || texture.Dimension != GTFDimension.Dimension2)
            throw new InvalidOperationException("Only 2D GTF textures are supported");

        ImageMetadata metadata = GetMetadata(texture);

        switch (texture.Format)
        {
            case GTFFormat.A8R8G8B8:
                // Unswizzle
                MortonSwizzle swizzle = new(texture.Width, texture.Height, 4);
                imgData = swizzle.Unswizzle(imgData);

                // Convert ARGB to BGRA
                for (int i = 0; i < imgData.Length; i += 4)
                {
                    byte a = imgData[i + 0];
                    byte r = imgData[i + 1];
                    byte g = imgData[i + 2];
                    byte b = imgData[i + 3];

                    imgData[i + 0] = b;
                    imgData[i + 1] = g;
                    imgData[i + 2] = r;
                    imgData[i + 3] = a;
                }

                return new RawImageData(imgData, RawImageDataPixelFormat.Bgra32, GetMetadata(texture));

            case GTFFormat.COMPRESSED_DXT1:
                return new RawImageData(imgData, RawImageDataCompressedFormat.DXT1, metadata);

            case GTFFormat.COMPRESSED_DXT23:
                return new RawImageData(imgData, RawImageDataCompressedFormat.DXT3, metadata);

            case GTFFormat.COMPRESSED_DXT45:
                return new RawImageData(imgData, RawImageDataCompressedFormat.DXT5, metadata);

            default:
                throw new InvalidOperationException($"The GTF format {texture.Format} is not supported");
        }
    }

    public override ImageMetadata Encode(RawImageData data, Stream outputStream)
    {
        throw new InvalidOperationException();
    }
}