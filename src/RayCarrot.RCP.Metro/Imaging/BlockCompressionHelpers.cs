namespace RayCarrot.RCP.Metro.Imaging;

public static class BlockCompressionHelpers
{
    public static int GetBlockWidth(int width) => Math.Max(1, (width + 3) / 4);
    public static int GetBlockHeight(int height) => Math.Max(1, (height + 3) / 4);
    public static int GetBytesPerBlock(RawImageDataCompressedFormat compressedFormat) => compressedFormat switch
    {
        RawImageDataCompressedFormat.None => throw new InvalidOperationException("Non-compressed format"),
        RawImageDataCompressedFormat.DXT1 => 8,
        RawImageDataCompressedFormat.DXT3 => 16,
        RawImageDataCompressedFormat.DXT5 => 16,
        _ => throw new ArgumentOutOfRangeException(nameof(compressedFormat), compressedFormat, null)
    };

    public static int GetImageLength(RawImageDataCompressedFormat compressedFormat, int width, int height)
    {
        int blockWidth = GetBlockWidth(width);
        int blockHeight = GetBlockHeight(height);
        int bytesPerBlock = GetBytesPerBlock(compressedFormat);

        return blockWidth * blockHeight * bytesPerBlock;
    }
}