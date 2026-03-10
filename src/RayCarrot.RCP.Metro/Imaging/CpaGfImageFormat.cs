using System.Drawing;
using System.IO;
using System.Runtime.CompilerServices;
using BinarySerializer;
using BinarySerializer.OpenSpace;

namespace RayCarrot.RCP.Metro.Imaging;

public class CpaGfImageFormat : ImageFormat
{
    public CpaGfImageFormat(OpenSpaceSettings settings)
    {
        Settings = settings;
    }

    private OpenSpaceSettings Settings { get; }

    public override string Id => "CPA_GF";
    public override string Name => "GF";

    public override bool CanDecode => true;
    public override bool CanEncode => true;

    private void FlipY(byte[] sourceImgData, int sourceIndex, byte[] destImgData, int destIndex, int width, int height, int bytesPerPixel)
    {
        int length = width * height * bytesPerPixel;
        int rowPitch = width * bytesPerPixel;

        sourceIndex += length - rowPitch;

        for (int y = 0; y < height; y++)
        {
            Array.Copy(sourceImgData, sourceIndex, destImgData, destIndex, rowPitch);
            sourceIndex -= rowPitch;
            destIndex += rowPitch;
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private byte GetColorFromBits(int value, int count, int offset)
    {
        int bitsValue = BitHelpers.ExtractBits(value, count, offset);
        return (byte)Math.Round(bitsValue * (Byte.MaxValue / (float)((1 << count) - 1)));
    }

    private void EncodeImage(RawImageData data, int mipmapLevel, GF_Format gfFormat, byte[] gfImgData, int offset)
    {
        byte[] rawData = data.GetImageData(mipmapLevel);

        int width = data.Width;
        int height = data.Height;

        switch (gfFormat)
        {
            case GF_Format.BGRA_8888 when data.PixelFormat is RawImageDataPixelFormat.Bgr24:
                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        gfImgData[offset + (y * width + x) * 4 + 0] = rawData[((height - y - 1) * width + x) * 3 + 0]; // B
                        gfImgData[offset + (y * width + x) * 4 + 1] = rawData[((height - y - 1) * width + x) * 3 + 1]; // G
                        gfImgData[offset + (y * width + x) * 4 + 2] = rawData[((height - y - 1) * width + x) * 3 + 2]; // R
                        gfImgData[offset + (y * width + x) * 4 + 3] = 0xFF; // A
                    }
                }
                break;

            case GF_Format.BGRA_8888 when data.PixelFormat is RawImageDataPixelFormat.Bgra32:
                FlipY(rawData, 0, gfImgData, offset, width, height, 4);
                break;

            case GF_Format.BGR_888 when data.PixelFormat is RawImageDataPixelFormat.Bgr24:
                FlipY(rawData, 0, gfImgData, offset, width, height, 3);
                break;

            case GF_Format.BGR_888 when data.PixelFormat is RawImageDataPixelFormat.Bgra32:
                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        gfImgData[offset + (y * width + x) * 3 + 0] = rawData[((height - y - 1) * width + x) * 4 + 0]; // B
                        gfImgData[offset + (y * width + x) * 3 + 1] = rawData[((height - y - 1) * width + x) * 4 + 1]; // G
                        gfImgData[offset + (y * width + x) * 3 + 2] = rawData[((height - y - 1) * width + x) * 4 + 2]; // R
                    }
                }
                break;

            case GF_Format.GrayscaleAlpha_88:
            case GF_Format.BGRA_4444:
            case GF_Format.BGRA_1555:
            case GF_Format.BGR_565:
            case GF_Format.BGRA_Indexed:
            case GF_Format.BGR_Indexed:
            case GF_Format.Grayscale:
                throw new NotImplementedException("Encoding 8-bit or 16-bit GF files is currently not supported");

            default:
                throw new ArgumentOutOfRangeException(nameof(gfFormat), gfFormat, null);
        }
    }

    private GFFile ReadGraphicsFile(Stream stream)
    {
        using Context context = new RCPContext(String.Empty);
        context.AddSettings(Settings);
        return context.ReadStreamData<GFFile>(stream, mode: VirtualFileMode.DoNotClose);
    }

    private GFFileHeader ReadGraphicsFileHeader(Stream stream)
    {
        using Context context = new RCPContext(String.Empty);
        context.AddSettings(Settings);
        return context.ReadStreamData<GFFileHeader>(stream, mode: VirtualFileMode.DoNotClose);
    }

    private void WriteGraphicsFile(Stream stream, GFFile gfFile)
    {
        using Context context = new RCPContext(String.Empty);
        context.AddSettings(Settings);
        context.WriteStreamData<GFFile>(stream, gfFile, mode: VirtualFileMode.DoNotClose);
    }

    private ImageMetadata GetMetadata(GFFileHeader gfHeader)
    {
        return new ImageMetadata(gfHeader.Width, gfHeader.Height);
    }

    public override ImageMetadata GetMetadata(Stream inputStream)
    {
        GFFileHeader gfHeader = ReadGraphicsFileHeader(inputStream);
        return GetMetadata(gfHeader);
    }

    public override RawImageData Decode(Stream inputStream)
    {
        // Read the file
        GFFile gf = ReadGraphicsFile(inputStream);

        // Get the format
        GF_Format format = gf.Header.PixelFormat;
        int width = gf.Header.Width;
        int height = gf.Header.Height;

        Func<DuoGridItemViewModel[]> customInfoItemsFactory = () =>
        [
            new DuoGridItemViewModel(
                header: new ResourceLocString(nameof(Resources.Archive_FileInfo_Img_Encoding)),
                text: gf.Header.PixelFormat.ToString(),
                minUserLevel: UserLevel.Technical)
        ];

        // TODO: Pass in mipmaps
        switch (format)
        {
            case GF_Format.BGRA_8888:
            {
                byte[] rawImgData = new byte[width * height * 4];
                FlipY(gf.ImgData, 0, rawImgData, 0, width, height, 4);
                return new RawImageData(rawImgData, RawImageDataPixelFormat.Bgra32, width, height)
                {
                    CustomInfoItemsFactory = customInfoItemsFactory
                };
            }

            case GF_Format.BGR_888:
            {
                byte[] rawImgData = new byte[width * height * 3];
                FlipY(gf.ImgData, 0, rawImgData, 0, width, height, 3);
                return new RawImageData(rawImgData, RawImageDataPixelFormat.Bgr24, width, height)
                {
                    CustomInfoItemsFactory = customInfoItemsFactory
                };
            }

            case GF_Format.GrayscaleAlpha_88:
            {
                byte[] rawImgData = new byte[width * height * 4];

                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        rawImgData[(y * width + x) * 4 + 0] = gf.ImgData[((height - y - 1) * width + x) * 2 + 0]; // B
                        rawImgData[(y * width + x) * 4 + 1] = gf.ImgData[((height - y - 1) * width + x) * 2 + 0]; // G
                        rawImgData[(y * width + x) * 4 + 2] = gf.ImgData[((height - y - 1) * width + x) * 2 + 0]; // R
                        rawImgData[(y * width + x) * 4 + 3] = gf.ImgData[((height - y - 1) * width + x) * 2 + 1]; // A
                    }
                }

                return new RawImageData(rawImgData, RawImageDataPixelFormat.Bgra32, width, height)
                {
                    CustomInfoItemsFactory = customInfoItemsFactory
                };
            }

            case GF_Format.BGRA_4444:
            {
                byte[] rawImgData = new byte[width * height * 4];

                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        ushort value = BitConverter.ToUInt16(gf.ImgData, ((height - y - 1) * width + x) * 2);

                        rawImgData[(y * width + x) * 4 + 0] = GetColorFromBits(value, 4, 0); // B
                        rawImgData[(y * width + x) * 4 + 1] = GetColorFromBits(value, 4, 4); // G
                        rawImgData[(y * width + x) * 4 + 2] = GetColorFromBits(value, 4, 8); // R
                        rawImgData[(y * width + x) * 4 + 3] = GetColorFromBits(value, 4, 12); // A
                    }
                }

                return new RawImageData(rawImgData, RawImageDataPixelFormat.Bgra32, width, height)
                {
                    CustomInfoItemsFactory = customInfoItemsFactory
                };
            }

            case GF_Format.BGRA_1555:
            {
                byte[] rawImgData = new byte[width * height * 4];

                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        ushort value = BitConverter.ToUInt16(gf.ImgData, ((height - y - 1) * width + x) * 2);

                        rawImgData[(y * width + x) * 4 + 0] = GetColorFromBits(value, 5, 0); // B
                        rawImgData[(y * width + x) * 4 + 1] = GetColorFromBits(value, 5, 5); // G
                        rawImgData[(y * width + x) * 4 + 2] = GetColorFromBits(value, 5, 10); // R
                        rawImgData[(y * width + x) * 4 + 3] = GetColorFromBits(value, 1, 15); // A
                    }
                }

                return new RawImageData(rawImgData, RawImageDataPixelFormat.Bgra32, width, height)
                {
                    CustomInfoItemsFactory = customInfoItemsFactory
                };
            }

            case GF_Format.BGR_565:
            {
                byte[] rawImgData = new byte[width * height * 3];

                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        ushort value = BitConverter.ToUInt16(gf.ImgData, ((height - y - 1) * width + x) * 2);

                        rawImgData[(y * width + x) * 3 + 0] = GetColorFromBits(value, 5, 0); // B
                        rawImgData[(y * width + x) * 3 + 1] = GetColorFromBits(value, 6, 5); // G
                        rawImgData[(y * width + x) * 3 + 2] = GetColorFromBits(value, 5, 11); // R
                    }
                }

                return new RawImageData(rawImgData, RawImageDataPixelFormat.Bgr24, width, height)
                {
                    CustomInfoItemsFactory = customInfoItemsFactory
                };
            }

            case GF_Format.BGRA_Indexed:
            {
                byte[] rawImgData = new byte[width * height * 4];

                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        int gfIndex = (height - y - 1) * width + x;

                        rawImgData[(y * width + x) * 4 + 0] = gf.Palette[gf.ImgData[gfIndex] * gf.Header.PaletteDataAlignment + 0]; // B
                        rawImgData[(y * width + x) * 4 + 1] = gf.Palette[gf.ImgData[gfIndex] * gf.Header.PaletteDataAlignment + 1]; // G
                        rawImgData[(y * width + x) * 4 + 2] = gf.Palette[gf.ImgData[gfIndex] * gf.Header.PaletteDataAlignment + 2]; // R
                        rawImgData[(y * width + x) * 4 + 3] = gf.Palette[gf.ImgData[gfIndex] * gf.Header.PaletteDataAlignment + 3]; // A
                    }
                }

                return new RawImageData(rawImgData, RawImageDataPixelFormat.Bgra32, width, height)
                {
                    CustomInfoItemsFactory = customInfoItemsFactory
                };
            }

            case GF_Format.BGR_Indexed:
            {
                byte[] rawImgData = new byte[width * height * 3];

                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        int gfIndex = (height - y - 1) * width + x;

                        rawImgData[(y * width + x) * 3 + 0] = gf.Palette[gf.ImgData[gfIndex] * gf.Header.PaletteDataAlignment + 0]; // B
                        rawImgData[(y * width + x) * 3 + 1] = gf.Palette[gf.ImgData[gfIndex] * gf.Header.PaletteDataAlignment + 1]; // G
                        rawImgData[(y * width + x) * 3 + 2] = gf.Palette[gf.ImgData[gfIndex] * gf.Header.PaletteDataAlignment + 2]; // R
                    }
                }

                return new RawImageData(rawImgData, RawImageDataPixelFormat.Bgr24, width, height)
                {
                    CustomInfoItemsFactory = customInfoItemsFactory
                };
            }

            case GF_Format.Grayscale:
            {
                byte[] rawImgData = new byte[width * height * 3];

                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        rawImgData[(y * width + x) * 3 + 0] = gf.ImgData[(height - y - 1) * width + x]; // B
                        rawImgData[(y * width + x) * 3 + 1] = gf.ImgData[(height - y - 1) * width + x]; // G
                        rawImgData[(y * width + x) * 3 + 2] = gf.ImgData[(height - y - 1) * width + x]; // R
                    }
                }

                return new RawImageData(rawImgData, RawImageDataPixelFormat.Bgr24, width, height)
                {
                    CustomInfoItemsFactory = customInfoItemsFactory
                };
            }

            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    public override ImageMetadata Encode(RawImageData data, Stream outputStream)
    {
        // NOTE: This method should never be used, but if it is we just default to 24 or 32-bit
        return Encode(data, outputStream, true, data.PixelFormat switch
        {
            RawImageDataPixelFormat.Bgr24 => GF_Format.BGR_888,
            RawImageDataPixelFormat.Bgra32 => GF_Format.BGRA_8888,
            _ => throw new ArgumentOutOfRangeException()
        });
    }

    public ImageMetadata Encode(RawImageData data, Stream outputStream, bool generateMipmaps, GF_Format format)
    {
        if (Settings.MajorEngineVersion == MajorEngineVersion.Montreal)
            throw new NotImplementedException("Encoding GF files for Montreal games is currently not supported");

        // Create the file
        GFFile gfFile = new()
        {
            Header = new GFFileHeader()
            {
                PixelFormat = format,
                Width = data.Width,
                Height = data.Height,
            }
        };

        // Recalculate the mipmap levels
        if (generateMipmaps && gfFile.Header.SupportsMipmaps(Settings))
            gfFile.Header.RecalculateMipmapLevels();
        else
            gfFile.Header.MipmapLevels = 0;

        // Recalculate the size
        gfFile.Header.RecalculateImageSize();

        // Create the image data array
        gfFile.ImgData = new byte[gfFile.Header.BytesPerPixel * gfFile.Header.ImageSize];

        // Encode the main image
        EncodeImage(data, 0, gfFile.Header.PixelFormat, gfFile.ImgData, 0);

        // TODO: Normalize mipmap generation directly in RawImageData and use mipmaps from there if they exist
        // Encode mipmaps
        if (gfFile.Header.ExclusiveMipmapsCount > 0)
        {
            // Keep track of the offset
            int mipmapOffset = gfFile.Header.Width * gfFile.Header.Height * gfFile.Header.BytesPerPixel;

            // Create a bitmap to use for resizing
            using Bitmap bitmap = data.ToBitmap();
            
            // Generate every mipmap
            foreach ((int width, int height) in gfFile.Header.GetExclusiveMipmapSizes())
            {
                // Resize the image
                using Bitmap resizedBitmap = bitmap.Resize(width, height);

                // Encode the mipmap
                EncodeImage(new RawImageData(resizedBitmap), 0, gfFile.Header.PixelFormat, gfFile.ImgData, mipmapOffset);

                // Increase the index
                mipmapOffset += height * width * gfFile.Header.BytesPerPixel;
            }
        }

        // Recalculate the RLE code
        gfFile.Header.RecalculateRLECode(gfFile.ImgData);

        // Write the file
        WriteGraphicsFile(outputStream, gfFile);

        return GetMetadata(gfFile.Header);
    }
}