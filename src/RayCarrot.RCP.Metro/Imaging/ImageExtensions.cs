using System.Runtime.InteropServices;
using DirectXTexNet;

namespace RayCarrot.RCP.Metro.Imaging;

public static class ImageExtensions
{
    public static byte[] GetRawBytes(this Image img)
    {
        int bytesPerPixel = TexHelper.Instance.BitsPerPixel(img.Format) / 8;
        byte[] rawBytes = new byte[img.RowPitch * img.Height];

        // Copy directly if no padding
        if (img.RowPitch == img.Width * bytesPerPixel)
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
            for (int y = 0; y < img.Height; y++)
            {
                Marshal.Copy(
                    source: img.Pixels + y * (int)img.RowPitch,
                    destination: rawBytes,
                    startIndex: y * img.Width * bytesPerPixel,
                    length: img.Width * bytesPerPixel);
            }
        }

        return rawBytes;
    }

}