using System.Drawing;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace RayCarrot.RCP.Metro.Imaging;

// TODO: Save mipmaps here too
public class RawImageData
{
    public RawImageData(byte[] compressedData, RawImageDataCompressedFormat compressedFormat, ImageMetadata metadata)
    {
        CompressedData = compressedData;
        CompressedFormat = compressedFormat;

        switch (compressedFormat)
        {
            case RawImageDataCompressedFormat.None:
                throw new ArgumentException("The data is not compressed", nameof(compressedFormat));
            
            case RawImageDataCompressedFormat.DXT1:
            case RawImageDataCompressedFormat.DXT3:
            case RawImageDataCompressedFormat.DXT5:
                RawData = BlockCompressionHelpers.Decompress(compressedData, compressedFormat, metadata.Width, metadata.Height);
                PixelFormat = RawImageDataPixelFormat.Bgra32;
                break;
            
            default:
                throw new ArgumentOutOfRangeException(nameof(compressedFormat), compressedFormat, null);
        }

        Metadata = metadata;
    }

    public RawImageData(byte[] rawData, RawImageDataPixelFormat pixelFormat, ImageMetadata metadata)
    {
        CompressedData = null;
        CompressedFormat = RawImageDataCompressedFormat.None;
        RawData = rawData;
        PixelFormat = pixelFormat;
        Metadata = metadata;
    }

    public RawImageData(Bitmap bmp)
    {
        using BitmapLock bmpLock = new(bmp);

        CompressedData = null;
        CompressedFormat = RawImageDataCompressedFormat.None;
        RawData = bmpLock.Pixels;
        PixelFormat = bmp.PixelFormat switch
        {
            System.Drawing.Imaging.PixelFormat.Format24bppRgb => RawImageDataPixelFormat.Bgr24,
            System.Drawing.Imaging.PixelFormat.Format32bppArgb => RawImageDataPixelFormat.Bgra32,
            _ => throw new InvalidOperationException("Unsupported pixel format")
        };
        Metadata = new ImageMetadata(bmp.Width, bmp.Height);
    }

    // TODO-UPDATE: Use this when converting if it's to a format which supports compressed data to avoid re-compressing it
    public byte[]? CompressedData { get; }
    public RawImageDataCompressedFormat CompressedFormat { get; }

    public byte[] RawData { get; }
    public RawImageDataPixelFormat PixelFormat { get; }

    public ImageMetadata Metadata { get; }

    public int GetBitsPerPixel()
    {
        return PixelFormat switch
        {
            RawImageDataPixelFormat.Bgr24 => 24,
            RawImageDataPixelFormat.Bgra32 => 32,
            _ => throw new ArgumentOutOfRangeException()
        };
    }

    public int GetStride()
    {
        int bpp = GetBitsPerPixel();
        int step = bpp / 8;
        return Metadata.Width * step;
    }

    public PixelFormat GetWindowsPixelFormat()
    {
        return PixelFormat switch
        {
            RawImageDataPixelFormat.Bgr24 => PixelFormats.Bgr24,
            RawImageDataPixelFormat.Bgra32 => PixelFormats.Bgra32,
            _ => throw new ArgumentOutOfRangeException()
        };
    }

    public byte[] Convert(RawImageDataPixelFormat newPixelFormat)
    {
        if (PixelFormat == newPixelFormat)
            return RawData;

        switch (PixelFormat)
        {
            case RawImageDataPixelFormat.Bgr24 when newPixelFormat is RawImageDataPixelFormat.Bgra32:
            {
                byte[] convertedData = new byte[Metadata.Width * Metadata.Height * 4];

                int originalIndex = 0;
                int convertedIndex = 0;

                for (int y = 0; y < Metadata.Height; y++)
                {
                    for (int x = 0; x < Metadata.Width; x++)
                    {
                        convertedData[convertedIndex + 0] = RawData[originalIndex + 0];
                        convertedData[convertedIndex + 1] = RawData[originalIndex + 1];
                        convertedData[convertedIndex + 2] = RawData[originalIndex + 2];
                        convertedData[convertedIndex + 3] = 0xFF;

                        originalIndex += 3;
                        convertedIndex += 4;
                    }
                }

                return convertedData;
            }

            case RawImageDataPixelFormat.Bgra32 when newPixelFormat is RawImageDataPixelFormat.Bgr24:
            {
                // Currently unused so no need to implement
                throw new NotImplementedException();
            }

            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    public bool UtilizesAlpha()
    {
        switch (PixelFormat)
        {
            case RawImageDataPixelFormat.Bgra32:
                for (int y = 0; y < Metadata.Height; y++)
                {
                    for (int x = 0; x < Metadata.Width; x++)
                    {
                        if (RawData[(y * Metadata.Width + x) * 4 + 3] != Byte.MaxValue)
                            return true;
                    }
                }

                return false;

            case RawImageDataPixelFormat.Bgr24:
                return false;

            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    public Bitmap ToBitmap()
    {
        // Create the bitmap
        Bitmap bmp = new(Metadata.Width, Metadata.Height, PixelFormat switch
        {
            // A bit confusing since it says RGB, but it's actually all BGR
            RawImageDataPixelFormat.Bgr24 => System.Drawing.Imaging.PixelFormat.Format24bppRgb,
            RawImageDataPixelFormat.Bgra32 => System.Drawing.Imaging.PixelFormat.Format32bppArgb,
            _ => throw new ArgumentOutOfRangeException()
        });

        // Lock and update the pixels
        using var bmpLock = new BitmapLock(bmp);
        bmpLock.Pixels = RawData;

        // Return the bitmap
        return bmp;
    }

    public BitmapSource ToBitmapSource()
    {
        int stride = GetStride();
        PixelFormat format = GetWindowsPixelFormat();

        return BitmapSource.Create(Metadata.Width, Metadata.Height, 96, 96, format, null, RawData, stride);
    }
}