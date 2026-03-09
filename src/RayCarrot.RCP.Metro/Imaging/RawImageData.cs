using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace RayCarrot.RCP.Metro.Imaging;

public class RawImageData
{
    public RawImageData(
        MipmapImage[] compressedMipmaps, 
        RawImageDataCompressedFormat compressedFormat, 
        MipmapImage[] mipmaps, 
        RawImageDataPixelFormat pixelFormat, 
        ImageMetadata metadata)
    {
        if (compressedMipmaps.Length != mipmaps.Length)
            throw new ArgumentException("The amount of mipmaps don't match between the compressed and raw image data");

        CompressedMipmaps = compressedMipmaps;
        CompressedFormat = compressedFormat;
        Mipmaps = mipmaps;
        PixelFormat = pixelFormat;
        MipmapSizes = GenerateMipmapSizes(metadata.Width, metadata.Height, mipmaps.Length);
        Metadata = metadata;
    }

    public RawImageData(byte[] compressedData, RawImageDataCompressedFormat compressedFormat, ImageMetadata metadata) :
        this([new MipmapImage(compressedData)], compressedFormat, metadata) { }

    public RawImageData(MipmapImage[] compressedMipmaps, RawImageDataCompressedFormat compressedFormat, ImageMetadata metadata)
    {
        CompressedMipmaps = compressedMipmaps;
        CompressedFormat = compressedFormat;
        MipmapSizes = GenerateMipmapSizes(metadata.Width, metadata.Height, compressedMipmaps.Length);

        switch (compressedFormat)
        {
            case RawImageDataCompressedFormat.None:
                throw new ArgumentException("The data is not compressed", nameof(compressedFormat));
            
            // Block compression
            case RawImageDataCompressedFormat.DXT1:
            case RawImageDataCompressedFormat.DXT3:
            case RawImageDataCompressedFormat.DXT5:
                Mipmaps = new MipmapImage[CompressedMipmaps.Length];
                for (int i = 0; i < Mipmaps.Length; i++)
                {
                    byte[] imgData = compressedMipmaps[i].ImageData;
                    Size size = MipmapSizes[i];
                    Mipmaps[i] = new MipmapImage(BlockCompressionHelpers.Decompress(imgData, compressedFormat, size.Width, size.Height));
                }
                PixelFormat = RawImageDataPixelFormat.Bgra32;
                break;
            
            default:
                throw new ArgumentOutOfRangeException(nameof(compressedFormat), compressedFormat, null);
        }

        Metadata = metadata;
    }

    public RawImageData(byte[] rawData, RawImageDataPixelFormat pixelFormat, ImageMetadata metadata) :
        this([new MipmapImage(rawData)], pixelFormat, metadata) { }

    public RawImageData(MipmapImage[] mipmaps, RawImageDataPixelFormat pixelFormat, ImageMetadata metadata)
    {
        CompressedMipmaps = null;
        CompressedFormat = RawImageDataCompressedFormat.None;
        Mipmaps = mipmaps;
        PixelFormat = pixelFormat;
        MipmapSizes = GenerateMipmapSizes(metadata.Width, metadata.Height, mipmaps.Length);
        Metadata = metadata;
    }

    public RawImageData(Bitmap bmp)
    {
        using BitmapLock bmpLock = new(bmp);

        CompressedMipmaps = null;
        CompressedFormat = RawImageDataCompressedFormat.None;
        Mipmaps = [new MipmapImage(bmpLock.Pixels)];
        PixelFormat = bmp.PixelFormat switch
        {
            System.Drawing.Imaging.PixelFormat.Format24bppRgb => RawImageDataPixelFormat.Bgr24,
            System.Drawing.Imaging.PixelFormat.Format32bppArgb => RawImageDataPixelFormat.Bgra32,
            _ => throw new InvalidOperationException("Unsupported pixel format")
        };
        MipmapSizes = GenerateMipmapSizes(bmp.Width, bmp.Height, 1);
        Metadata = new ImageMetadata(bmp.Width, bmp.Height);
    }

    public MipmapImage[]? CompressedMipmaps { get; }
    public RawImageDataCompressedFormat CompressedFormat { get; }

    [MemberNotNullWhen(true, nameof(CompressedMipmaps))]
    public bool IsCompressed => CompressedFormat != RawImageDataCompressedFormat.None;

    [MemberNotNullWhen(true, nameof(CompressedMipmaps))]
    public bool IsBlockCompressed => CompressedFormat is 
        RawImageDataCompressedFormat.DXT1 or 
        RawImageDataCompressedFormat.DXT3 or 
        RawImageDataCompressedFormat.DXT5;

    public MipmapImage[] Mipmaps { get; }
    public RawImageDataPixelFormat PixelFormat { get; }

    public Size[] MipmapSizes { get; }
    public bool HasMipmaps => MipmapLevels > 1;
    public int MipmapLevels => MipmapSizes.Length;

    public ImageMetadata Metadata { get; }

    private static Size[] GenerateMipmapSizes(int width, int height, int mipmapLevels)
    {
        Size[] sizes = new Size[mipmapLevels];

        for (int i = 0; i < mipmapLevels; i++)
        {
            sizes[i] = new Size(width, height);

            width = Math.Max(1, width >> 1);
            height = Math.Max(1, height >> 1);
        }

        return sizes;
    }

    public byte[] GetImageData(int mipmapLevel)
    {
        return Mipmaps[mipmapLevel].ImageData;
    }

    public byte[] GetCompressedImageData(int mipmapLevel)
    {
        if (!IsCompressed)
            throw new InvalidOperationException("There is no compressed data");

        return CompressedMipmaps[mipmapLevel].ImageData;
    }

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

    public byte[] Convert(RawImageDataPixelFormat newPixelFormat, int mipmapLevel)
    {
        byte[] imgData = GetImageData(mipmapLevel);

        if (PixelFormat == newPixelFormat)
            return Mipmaps[mipmapLevel].ImageData;

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
                        convertedData[convertedIndex + 0] = imgData[originalIndex + 0];
                        convertedData[convertedIndex + 1] = imgData[originalIndex + 1];
                        convertedData[convertedIndex + 2] = imgData[originalIndex + 2];
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
        byte[] imgData = GetImageData(0);

        switch (PixelFormat)
        {
            case RawImageDataPixelFormat.Bgra32:
                for (int y = 0; y < Metadata.Height; y++)
                {
                    for (int x = 0; x < Metadata.Width; x++)
                    {
                        if (imgData[(y * Metadata.Width + x) * 4 + 3] != Byte.MaxValue)
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
        byte[] imgData = GetImageData(0);

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
        bmpLock.Pixels = imgData;

        // Return the bitmap
        return bmp;
    }

    public BitmapSource ToBitmapSource()
    {
        byte[] imgData = GetImageData(0);

        int stride = GetStride();
        PixelFormat format = GetWindowsPixelFormat();

        return BitmapSource.Create(Metadata.Width, Metadata.Height, 96, 96, format, null, imgData, stride);
    }

    public RawImageData WithoutCompressedData()
    {
        return new RawImageData(Mipmaps, PixelFormat, Metadata);
    }
}