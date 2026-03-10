using System.IO;
using BinarySerializer;
using BinarySerializer.UbiArt;

namespace RayCarrot.RCP.Metro.Imaging;

public class Xbox360D3DTextureImageFormat : ImageFormat
{
    public override string Id => "Xbox360_D3DTexture";
    public override string Name => "D3DTexture";

    public override bool CanDecode => true;
    public override bool CanEncode => false;

    private ImageMetadata GetMetadata(D3DTexture tex)
    {
        return new ImageMetadata(tex.ActualWidth, tex.ActualHeight);
    }

    public override ImageMetadata GetMetadata(Stream inputStream)
    {
        using Context context = new RCPContext(String.Empty);
        D3DTexture header = context.ReadStreamData<D3DTexture>(inputStream, endian: Endian.Big, mode: VirtualFileMode.DoNotClose, maintainPosition: true);
        return GetMetadata(header);
    }

    public override RawImageData Decode(Stream inputStream)
    {
        // Read the header
        using Context context = new RCPContext(String.Empty);
        D3DTexture texture = context.ReadStreamData<D3DTexture>(inputStream, endian: Endian.Big, mode: VirtualFileMode.DoNotClose, maintainPosition: true);

        // TODO: Can we determine the length from the header instead?
        // Read the raw image data
        byte[] imgData = new byte[inputStream.Length - inputStream.Position];
        inputStream.Read(imgData, 0, imgData.Length);

        // TODO: Use this code instead: https://github.com/xenia-project/xenia/blob/master/src/xenia/gpu/texture_util.cc
        // Untile the image data
        imgData = texture.Untile(imgData);

        Func<DuoGridItemViewModel[]> customInfoItemsFactory = () =>
        [
            new DuoGridItemViewModel(
                header: new ResourceLocString(nameof(Resources.Archive_FileInfo_Img_Encoding)),
                text: texture.DataFormat.ToString(),
                minUserLevel: UserLevel.Technical)
        ];

        // Remove mipmaps for now
        Array.Resize(ref imgData, texture.DataFormat switch
        {
            D3DTexture.GPUTEXTUREFORMAT.GPUTEXTUREFORMAT_8_8_8_8 => 
                texture.ActualWidth * texture.ActualHeight * 4,
            D3DTexture.GPUTEXTUREFORMAT.GPUTEXTUREFORMAT_DXT1 => 
                BlockCompressionHelpers.GetImageLength(RawImageDataCompressedFormat.DXT1, texture.ActualWidth, texture.ActualHeight),
            D3DTexture.GPUTEXTUREFORMAT.GPUTEXTUREFORMAT_DXT2_3 => 
                BlockCompressionHelpers.GetImageLength(RawImageDataCompressedFormat.DXT3, texture.ActualWidth, texture.ActualHeight),
            D3DTexture.GPUTEXTUREFORMAT.GPUTEXTUREFORMAT_DXT4_5 => 
                BlockCompressionHelpers.GetImageLength(RawImageDataCompressedFormat.DXT5, texture.ActualWidth, texture.ActualHeight),
            _ => throw new InvalidOperationException($"The D3D format {texture.DataFormat} is not supported"),
        });

        // TODO: Pass in mipmaps
        switch (texture.DataFormat)
        {
            case D3DTexture.GPUTEXTUREFORMAT.GPUTEXTUREFORMAT_8_8_8_8:
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

                return new RawImageData(imgData, RawImageDataPixelFormat.Bgra32, texture.ActualWidth, texture.ActualHeight)
                {
                    CustomInfoItemsFactory = customInfoItemsFactory
                };

            case D3DTexture.GPUTEXTUREFORMAT.GPUTEXTUREFORMAT_DXT1:
                return new RawImageData(imgData, RawImageDataCompressedFormat.DXT1, texture.ActualWidth, texture.ActualHeight)
                {
                    CustomInfoItemsFactory = customInfoItemsFactory
                };

            case D3DTexture.GPUTEXTUREFORMAT.GPUTEXTUREFORMAT_DXT2_3:
                return new RawImageData(imgData, RawImageDataCompressedFormat.DXT3, texture.ActualWidth, texture.ActualHeight)
                {
                    CustomInfoItemsFactory = customInfoItemsFactory
                };

            case D3DTexture.GPUTEXTUREFORMAT.GPUTEXTUREFORMAT_DXT4_5:
                return new RawImageData(imgData, RawImageDataCompressedFormat.DXT5, texture.ActualWidth, texture.ActualHeight)
                {
                    CustomInfoItemsFactory = customInfoItemsFactory
                };

            default:
                throw new InvalidOperationException($"The D3D format {texture.DataFormat} is not supported");
        }
    }

    public override ImageMetadata Encode(RawImageData data, Stream outputStream)
    {
        throw new InvalidOperationException();
    }
}