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
        RawImageDataPixelFormat pixelFormat)
    {
        if (compressedMipmaps.Length != mipmaps.Length)
            throw new ArgumentException("The amount of mipmaps don't match between the compressed and raw image data");

        CompressedMipmaps = compressedMipmaps;
        CompressedFormat = compressedFormat;
        Mipmaps = mipmaps;
        PixelFormat = pixelFormat;
    }

    public RawImageData(byte[] compressedData, RawImageDataCompressedFormat compressedFormat, int width, int height) :
        this([new MipmapImage(compressedData, width, height)], compressedFormat) { }

    public RawImageData(MipmapImage[] compressedMipmaps, RawImageDataCompressedFormat compressedFormat)
    {
        CompressedMipmaps = compressedMipmaps;
        CompressedFormat = compressedFormat;

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
                    MipmapImage img = compressedMipmaps[i];
                    Mipmaps[i] = new MipmapImage(BlockCompressionHelpers.Decompress(img.ImageData, compressedFormat, img.Width, img.Height), img.Width, img.Height);
                }
                PixelFormat = RawImageDataPixelFormat.Bgra32;
                break;
            
            default:
                throw new ArgumentOutOfRangeException(nameof(compressedFormat), compressedFormat, null);
        }
    }

    public RawImageData(byte[] rawData, RawImageDataPixelFormat pixelFormat, int width, int height) :
        this([new MipmapImage(rawData, width, height)], pixelFormat) { }

    public RawImageData(MipmapImage[] mipmaps, RawImageDataPixelFormat pixelFormat)
    {
        CompressedMipmaps = null;
        CompressedFormat = RawImageDataCompressedFormat.None;
        Mipmaps = mipmaps;
        PixelFormat = pixelFormat;
    }

    public RawImageData(Bitmap bmp)
    {
        using BitmapLock bmpLock = new(bmp);

        CompressedMipmaps = null;
        CompressedFormat = RawImageDataCompressedFormat.None;
        Mipmaps = [new MipmapImage(bmpLock.Pixels, bmp.Width, bmp.Height)];
        PixelFormat = bmp.PixelFormat switch
        {
            System.Drawing.Imaging.PixelFormat.Format24bppRgb => RawImageDataPixelFormat.Bgr24,
            System.Drawing.Imaging.PixelFormat.Format32bppArgb => RawImageDataPixelFormat.Bgra32,
            _ => throw new InvalidOperationException("Unsupported pixel format")
        };
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

    public int Width => GetImageSize(0).Width;
    public int Height => GetImageSize(0).Height;

    public bool HasMipmaps => MipmapLevels > 1;
    public int MipmapLevels => Mipmaps.Length;

    public Func<DuoGridItemViewModel[]>? CustomInfoItemsFactory { private get; init; }

    private int GetClosestMipmapLevel(int requestedWidth, int requestedHeight)
    {
        if (requestedWidth <= 0 || requestedHeight <= 0)
            return 0;

        int mipmapLevel = 0;
        int bestDist = Int32.MaxValue;

        for (int i = 0; i < MipmapLevels; i++)
        {
            Size size = GetImageSize(i);
            
            int dw = size.Width - requestedWidth;
            int dh = size.Height - requestedHeight;
            int dist = dw * dw + dh * dh;

            if (dist < bestDist)
            {
                bestDist = dist;
                mipmapLevel = i;
            }
        }

        return mipmapLevel;
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

    public Size GetImageSize(int mipmapLevel)
    {
        MipmapImage img = Mipmaps[mipmapLevel];
        return new Size(img.Width, img.Height);
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

    public int GetStride(int mipmapLevel)
    {
        int bpp = GetBitsPerPixel();
        int step = bpp / 8;
        return GetImageSize(mipmapLevel).Width * step;
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
                byte[] convertedData = new byte[Width * Height * 4];

                int originalIndex = 0;
                int convertedIndex = 0;

                for (int y = 0; y < Height; y++)
                {
                    for (int x = 0; x < Width; x++)
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
                for (int y = 0; y < Height; y++)
                {
                    for (int x = 0; x < Width; x++)
                    {
                        if (imgData[(y * Width + x) * 4 + 3] != Byte.MaxValue)
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
        Bitmap bmp = new(Width, Height, PixelFormat switch
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

    public BitmapSource ToBitmapSource(int requestedWidth, int requestedHeight)
    {
        int mipmapLevel = GetClosestMipmapLevel(requestedWidth, requestedHeight);

        byte[] imgData = GetImageData(mipmapLevel);
        Size size = GetImageSize(mipmapLevel);

        int stride = GetStride(mipmapLevel);
        PixelFormat format = GetWindowsPixelFormat();

        return BitmapSource.Create(size.Width, size.Height, 96, 96, format, null, imgData, stride);
    }

    public RawImageData WithoutCompressedData()
    {
        return new RawImageData(Mipmaps, PixelFormat);
    }

    public IEnumerable<DuoGridItemViewModel> GetInfoItems()
    {
        yield return new DuoGridItemViewModel(
            header: new ResourceLocString(nameof(Resources.Archive_FileInfo_Img_Size)),
            text: $"{Width}x{Height}");

        if (HasMipmaps)
            yield return new DuoGridItemViewModel(
                header: new ResourceLocString(nameof(Resources.Archive_FileInfo_Img_Mipmaps)),
                text: MipmapLevels.ToString(),
                minUserLevel: UserLevel.Technical);

        if (CustomInfoItemsFactory != null)
        {
            foreach (DuoGridItemViewModel item in CustomInfoItemsFactory())
                yield return item;
        }
    }
}