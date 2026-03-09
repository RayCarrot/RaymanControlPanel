using System.Runtime.InteropServices;
using DirectXTexNet;

namespace RayCarrot.RCP.Metro.Imaging;

public static class ImageExtensions
{
    public static byte[] GetRawBytes(this Image img)
    {
        bool compressed = TexHelper.Instance.IsCompressed(img.Format);

        int rowSize;
        int rows;
        if (compressed)
        {
            int blockWidth = BlockCompressionHelpers.GetBlockWidth(img.Width);
            int blockHeight = BlockCompressionHelpers.GetBlockWidth(img.Height);

            int bytesPerBlock = TexHelper.Instance.BitsPerPixel(img.Format) * (16 / 8);

            rowSize = blockWidth * bytesPerBlock;
            rows = blockHeight;
        }
        else
        {
            int bytesPerPixel = TexHelper.Instance.BitsPerPixel(img.Format) / 8;

            rowSize = img.Width * bytesPerPixel;
            rows = img.Height;
        }

        byte[] rawBytes = new byte[rowSize * rows];

        // Copy directly if no padding
        if (img.RowPitch == rowSize)
        {
            Marshal.Copy(
                source: img.Pixels,
                destination: rawBytes,
                startIndex: 0,
                length: rawBytes.Length);
        }
        // Copy row by row
        else
        {
            for (int y = 0; y < rows; y++)
            {
                Marshal.Copy(
                    source: img.Pixels + y * (int)img.RowPitch,
                    destination: rawBytes,
                    startIndex: y * rowSize,
                    length: rowSize);
            }
        }

        return rawBytes;
    }

}