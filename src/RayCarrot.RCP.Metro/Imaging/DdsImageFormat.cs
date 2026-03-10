using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using DirectXTexNet;
using Image = DirectXTexNet.Image;

namespace RayCarrot.RCP.Metro.Imaging;

public class DdsImageFormat : ImageFormat
{
    public override string Id => "DDS";
    public override string Name => "DDS";

    public override bool CanDecode => true;
    public override bool CanEncode => true;

    public override FileExtension[] FileExtensions { get; } =
    {
        new(".dds"),
    };

    private static ScratchImage Compress(ScratchImage scratchImage, DXGI_FORMAT format)
    {
        const TEX_COMPRESS_FLAGS compFlags = TEX_COMPRESS_FLAGS.PARALLEL;
        return scratchImage.Compress(format, compFlags, 0.5f);
    }

    private static MipmapImage[] GetMipmapImages(ScratchImage scratchImage)
    {
        int mipmapLevels = scratchImage.GetMetadata().MipLevels;
        MipmapImage[] mipmaps = new MipmapImage[mipmapLevels];
        for (int i = 0; i < mipmapLevels; i++)
        {
            Image image = scratchImage.GetImage(i);
            byte[] rawData = image.GetRawBytes();
            mipmaps[i] = new MipmapImage(rawData, image.Width, image.Height);
        }
        return mipmaps;
    }

    private static ScratchImage GenerateMipmaps(ScratchImage scratchImage)
    {
        // Separate alpha to avoid alpha blending issues for mipmaps
        const TEX_FILTER_FLAGS filterFlags = TEX_FILTER_FLAGS.BOX | TEX_FILTER_FLAGS.SEPARATE_ALPHA;

        // Generate mipmaps for all level (0 does for all levels down to 1x1)
        return scratchImage.GenerateMipMaps(filterFlags, 0);
    }

    private static UnmanagedData<Image> CreateImage(RawImageData data, int mipmapLevel)
    {
        // Get the size
        Size size = data.GetImageSize(mipmapLevel);

        // If the data is block compressed then we want to maintain that to avoid re-compressing it
        DXGI_FORMAT sourceFormat;
        byte[] sourceData;
        if (data.IsBlockCompressed)
        {
            sourceFormat = data.CompressedFormat switch
            {
                RawImageDataCompressedFormat.DXT1 => DXGI_FORMAT.BC1_UNORM,
                RawImageDataCompressedFormat.DXT3 => DXGI_FORMAT.BC2_UNORM,
                RawImageDataCompressedFormat.DXT5 => DXGI_FORMAT.BC3_UNORM,
                _ => throw new ArgumentException("The image is not block compressed", nameof(data.CompressedFormat))
            };
            sourceData = data.GetCompressedImageData(mipmapLevel);
        }
        else
        {
            sourceFormat = DXGI_FORMAT.B8G8R8A8_UNORM;
            sourceData = data.PixelFormat switch
            {
                RawImageDataPixelFormat.Bgr24 => data.Convert(RawImageDataPixelFormat.Bgra32, mipmapLevel),
                RawImageDataPixelFormat.Bgra32 => data.GetImageData(mipmapLevel),
                _ => throw new ArgumentOutOfRangeException(nameof(data.PixelFormat), data.PixelFormat, null)
            };
        }

        // Compute the row and slice pitch
        TexHelper.Instance.ComputePitch(sourceFormat, size.Width, size.Height, out long rowPitch, out long slicePitch, CP_FLAGS.NONE);

        return new UnmanagedData<Image>(sourceData, ptr => new Image(
            width: size.Width,
            height: size.Height,
            format: sourceFormat,
            rowPitch: rowPitch,
            slicePitch: slicePitch,
            pixels: ptr,
            parent: null));
    }

    private static ImageMetadata GetMetadata(TexMetadata metadata)
    {
        return new ImageMetadata(metadata.Width, metadata.Height);
    }

    public override ImageMetadata GetMetadata(Stream inputStream)
    {
        // We only need to read the header
        const int headerSize = 4 + 124; // Magic + DDS_HEADER structure
        byte[] imgData = new byte[headerSize];
        inputStream.Read(imgData, 0, imgData.Length);

        // Allocate and get pointer
        IntPtr imgDataPtr = Marshal.AllocHGlobal(imgData.Length);
        try
        {
            Marshal.Copy(
                source: imgData,
                startIndex: 0,
                destination: imgDataPtr,
                length: imgData.Length);

            TexMetadata metadata = TexHelper.Instance.GetMetadataFromDDSMemory(imgDataPtr, imgData.Length, DDS_FLAGS.NONE);

            return GetMetadata(metadata);
        }
        finally
        {
            Marshal.FreeHGlobal(imgDataPtr);
        }
    }

    public override RawImageData Decode(Stream inputStream)
    {
        // TODO: Not great to determine the length like this. Maybe we should pass in byte array to Decode instead
        //       of stream? Although we're mainly dealing with streams from the Archive Explorer.
        byte[] imgData = new byte[inputStream.Length - inputStream.Position];
        inputStream.Read(imgData, 0, imgData.Length);

        // Allocate and get pointer
        IntPtr imgDataPtr = Marshal.AllocHGlobal(imgData.Length);
        try
        {
            Marshal.Copy(
                source: imgData, 
                startIndex: 0, 
                destination: imgDataPtr, 
                length: imgData.Length);

            using ScratchImage scratchImg = TexHelper.Instance.LoadFromDDSMemory(imgDataPtr, imgData.Length, DDS_FLAGS.NONE);

            TexMetadata metadata = scratchImg.GetMetadata();

            Func<DuoGridItemViewModel[]> customInfoItemsFactory = () =>
            [
                new DuoGridItemViewModel(
                    header: new ResourceLocString(nameof(Resources.Archive_FileInfo_Img_Encoding)),
                    text: metadata.Format.ToString(),
                    minUserLevel: UserLevel.Technical)
            ];

            // If it's block compressed
            if (metadata.Format is DXGI_FORMAT.BC1_UNORM or DXGI_FORMAT.BC2_UNORM or DXGI_FORMAT.BC3_UNORM)
            {
                MipmapImage[] compressedMipmaps = GetMipmapImages(scratchImg);

                RawImageDataCompressedFormat compressedFormat = metadata.Format switch
                {
                    DXGI_FORMAT.BC1_UNORM => RawImageDataCompressedFormat.DXT1,
                    DXGI_FORMAT.BC2_UNORM => RawImageDataCompressedFormat.DXT3,
                    DXGI_FORMAT.BC3_UNORM => RawImageDataCompressedFormat.DXT5,
                    _ => throw new ArgumentException("The image is not block compressed", nameof(metadata.Format))
                };

                return new RawImageData(compressedMipmaps, compressedFormat)
                {
                    CustomInfoItemsFactory = customInfoItemsFactory
                };
            }
            // If it's already in the correct format
            else if (metadata.Format == DXGI_FORMAT.B8G8R8A8_UNORM)
            {
                MipmapImage[] mipmaps = GetMipmapImages(scratchImg);

                return new RawImageData(mipmaps, RawImageDataPixelFormat.Bgra32)
                {
                    CustomInfoItemsFactory = customInfoItemsFactory
                };
            }
            // If in a different format
            else
            {
                int mipmapLevels = metadata.MipLevels;
                MipmapImage[] mipmaps = new MipmapImage[mipmapLevels];
                for (int i = 0; i < mipmapLevels; i++)
                {
                    using ScratchImage mipScratchImg = TexHelper.Instance.IsCompressed(metadata.Format) 
                        ? scratchImg.Decompress(i, DXGI_FORMAT.B8G8R8A8_UNORM) 
                        : scratchImg.Convert(i, DXGI_FORMAT.B8G8R8A8_UNORM, TEX_FILTER_FLAGS.DEFAULT, 0.5f);

                    Image image = mipScratchImg.GetImage(0);
                    byte[] rawData = image.GetRawBytes();
                    mipmaps[i] = new MipmapImage(rawData, image.Width, image.Height);
                }

                return new RawImageData(mipmaps, RawImageDataPixelFormat.Bgra32)
                {
                    CustomInfoItemsFactory = customInfoItemsFactory
                };
            }
        }
        finally
        {
            Marshal.FreeHGlobal(imgDataPtr);
        }
    }

    public override ImageMetadata Encode(RawImageData data, Stream outputStream)
    {
        int width = data.Width;
        int height = data.Height;

        if (!width.IsPowerOfTwo() || !height.IsPowerOfTwo())
            throw new Exception("In order to ensure full compatibility and to generate mipmaps the image must have dimensions which are a power of 2, such as 128, 256, 512, 1024 etc.");

        ScratchImage? scratchImage = null;
        List<UnmanagedData<Image>> mipmapImages = new();
        try
        {
            // Create an image for every mipmap
            for (int i = 0; i < data.MipmapLevels; i++)
                mipmapImages.Add(CreateImage(data, i));

            // Get the format from the first image
            DXGI_FORMAT format = mipmapImages[0].Resource.Format;

            TexMetadata texMetadata = new(
                width: width,
                height: height,
                depth: 1,
                arraySize: 1,
                mipLevels: mipmapImages.Count,
                miscFlags: 0,
                miscFlags2: 0,
                format: format,
                dimension: TEX_DIMENSION.TEXTURE2D);

            scratchImage = TexHelper.Instance.InitializeTemporary(mipmapImages.Select(x => x.Resource).ToArray(), texMetadata, null);

            bool isCompressed = TexHelper.Instance.IsCompressed(format);

            // Generate mipmaps if the format is not compressed and we don't already have mipmaps
            if (!isCompressed && mipmapImages.Count == 1)
            {
                ScratchImage mip = GenerateMipmaps(scratchImage);
                scratchImage.Dispose();
                scratchImage = mip;
            }

            // Compress if not already compressed
            if (!isCompressed)
            {
                // Compress to either DXT1 or DXT5
                bool isTransparent = data.UtilizesAlpha();
                DXGI_FORMAT compressedFormat = !isTransparent
                    ? DXGI_FORMAT.BC1_UNORM  // DXT1
                    : DXGI_FORMAT.BC3_UNORM; // DXT5

                ScratchImage comp = Compress(scratchImage, compressedFormat);
                scratchImage.Dispose();
                scratchImage = comp;
            }

            using UnmanagedMemoryStream ddsStream = scratchImage.SaveToDDSMemory(DDS_FLAGS.NONE);
            ddsStream.CopyToEx(outputStream);

            return GetMetadata(scratchImage.GetMetadata());
        }
        finally
        {
            scratchImage?.Dispose();
            foreach (UnmanagedData<Image> mipmapImg in mipmapImages)
                mipmapImg.Dispose();
        }
    }
}