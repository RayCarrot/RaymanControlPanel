using System.Runtime.InteropServices;
using DirectXTexNet;

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

    public static byte[] Decompress(byte[] compressedData, RawImageDataCompressedFormat compressedFormat, int width, int height)
    {
        // Get the format
        DXGI_FORMAT format = compressedFormat switch
        {
            RawImageDataCompressedFormat.DXT1 => DXGI_FORMAT.BC1_UNORM,
            RawImageDataCompressedFormat.DXT3 => DXGI_FORMAT.BC2_UNORM,
            RawImageDataCompressedFormat.DXT5 => DXGI_FORMAT.BC3_UNORM,
            _ => throw new ArgumentException("The image is not block compressed", nameof(compressedFormat))
        };

        // Get sizes
        int blockWidth = GetBlockWidth(width);
        int blockHeight = GetBlockHeight(height);
        int bytesPerBlock = GetBytesPerBlock(compressedFormat);

        int rowPitch = blockWidth * bytesPerBlock;
        int slicePitch = rowPitch * blockHeight;

        // Verify the size matches
        if (slicePitch != compressedData.Length)
            throw new Exception("Compressed data length is wrong");

        IntPtr rawDataPtr = Marshal.AllocHGlobal(slicePitch);
        try
        {
            Marshal.Copy(compressedData, 0, rawDataPtr, compressedData.Length);

            Image img = new(
                width: width,
                height: height,
                format: format,
                rowPitch: rowPitch,
                slicePitch: slicePitch,
                pixels: rawDataPtr,
                parent: null);

            TexMetadata texMetadata = new(
                width: img.Width,
                height: img.Height,
                depth: 1,
                arraySize: 1,
                mipLevels: 1,
                miscFlags: 0,
                miscFlags2: 0,
                format: img.Format,
                dimension: TEX_DIMENSION.TEXTURE2D);

            using ScratchImage scratchImage = TexHelper.Instance.InitializeTemporary([img], texMetadata, null);

            // Decompress the image
            using ScratchImage bgraScratchImg = scratchImage.Decompress(0, DXGI_FORMAT.B8G8R8A8_UNORM);

            // Get the primary image
            Image primaryImg = bgraScratchImg.GetImage(0);

            // Get the raw bytes
            return primaryImg.GetRawBytes();
        }
        finally
        {
            Marshal.FreeHGlobal(rawDataPtr);
        }
    }
}