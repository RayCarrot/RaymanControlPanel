using System.IO;
using BinarySerializer;
using BinarySerializer.Nintendo.Switch;

namespace RayCarrot.RCP.Metro.Imaging;

public class XtxImageFormat : ImageFormat
{
    public override string Id => "XTX";
    public override string Name => "XTX";

    public override bool CanDecode => true;
    public override bool CanEncode => false;

    public override FileExtension[] FileExtensions { get; } =
    {
        new(".xtx"),
    };


    private ImageMetadata GetMetadata(XTXTextureInfo textureInfo)
    {
        return new ImageMetadata((int)textureInfo.Width, (int)textureInfo.Height);
    }

    public override ImageMetadata GetMetadata(Stream inputStream)
    {
        // Read the file
        using Context context = new RCPContext(String.Empty);
        XTXTexture xtx = context.ReadStreamData<XTXTexture>(inputStream, mode: VirtualFileMode.DoNotClose, maintainPosition: true);

        XTXTextureInfo textureInfo = xtx.Blocks.First(x => x.BlockType == XTXBlockType.Texture).TextureInfo;

        return GetMetadata(textureInfo);
    }

    public override RawImageData Decode(Stream inputStream)
    {
        // Read the file
        using Context context = new RCPContext(String.Empty);
        XTXTexture xtx = context.ReadStreamData<XTXTexture>(inputStream, mode: VirtualFileMode.DoNotClose, maintainPosition: true);

        XTXTextureInfo textureInfo = xtx.Blocks.First(x => x.BlockType == XTXBlockType.Texture).TextureInfo;
        byte[] imgData = xtx.Blocks.First(x => x.BlockType == XTXBlockType.Data).RawData;

        if (textureInfo.Depth != 1)
            throw new InvalidOperationException("Only 2D XTX textures are supported");

        int width = (int)textureInfo.Width;
        int height = (int)textureInfo.Height;

        // TODO: Support uncompressed format
        // TODO: Add info item for format
        // TODO: Pass in mipmaps
        TegraX1Swizzle swizzle;
        switch (textureInfo.Format)
        {
            case XTXImageFormat.DXT1:
                swizzle = new(
                    width: BlockCompressionHelpers.GetBlockWidth(width), 
                    height: BlockCompressionHelpers.GetBlockHeight(height), 
                    bytesPerPixel: BlockCompressionHelpers.GetBytesPerBlock(RawImageDataCompressedFormat.DXT1));
                imgData = swizzle.Unswizzle(imgData);

                return new RawImageData(imgData, RawImageDataCompressedFormat.DXT1, width, height);

            case XTXImageFormat.DXT3:
                swizzle = new(
                    width: BlockCompressionHelpers.GetBlockWidth(width),
                    height: BlockCompressionHelpers.GetBlockHeight(height),
                    bytesPerPixel: BlockCompressionHelpers.GetBytesPerBlock(RawImageDataCompressedFormat.DXT3));
                imgData = swizzle.Unswizzle(imgData);

                return new RawImageData(imgData, RawImageDataCompressedFormat.DXT3, (int)textureInfo.Width, (int)textureInfo.Height);

            case XTXImageFormat.DXT5:
                swizzle = new(
                    width: BlockCompressionHelpers.GetBlockWidth(width),
                    height: BlockCompressionHelpers.GetBlockHeight(height),
                    bytesPerPixel: BlockCompressionHelpers.GetBytesPerBlock(RawImageDataCompressedFormat.DXT5));
                imgData = swizzle.Unswizzle(imgData);

                return new RawImageData(imgData, RawImageDataCompressedFormat.DXT5, (int)textureInfo.Width, (int)textureInfo.Height);

            default:
                throw new InvalidOperationException($"The XTX format {textureInfo.Format} is not supported");
        }
    }

    public override ImageMetadata Encode(RawImageData data, Stream outputStream)
    {
        throw new InvalidOperationException();
    }
}