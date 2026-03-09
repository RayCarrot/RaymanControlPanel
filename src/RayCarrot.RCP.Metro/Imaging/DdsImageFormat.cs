using System.IO;
using System.Runtime.InteropServices;
using DirectXTexNet;

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

    private ScratchImage ConvertAndDecompress(ScratchImage scratchImg, DXGI_FORMAT format)
    {
        DXGI_FORMAT currentFormat = scratchImg.GetMetadata().Format;

        if (currentFormat == format)
            return scratchImg;
        else if (TexHelper.Instance.IsCompressed(currentFormat))
            return scratchImg.Decompress(0, format);
        else
            return scratchImg.Convert(0, format, TEX_FILTER_FLAGS.DEFAULT, 0.5f);
    }

    private ImageMetadata GetMetadata(TexMetadata metadata)
    {
        return new ImageMetadata(metadata.Width, metadata.Height)
        {
            MipmapsCount = metadata.MipLevels,
            Encoding = metadata.Format.ToString(),
        };
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

            // If block compressed...
            if (metadata.Format is DXGI_FORMAT.BC1_UNORM or DXGI_FORMAT.BC2_UNORM or DXGI_FORMAT.BC3_UNORM)
            {
                // Get the compressed data
                byte[] compressedData = scratchImg.GetImage(0).GetRawBytes();

                RawImageDataCompressedFormat compressedFormat = metadata.Format switch
                {
                    DXGI_FORMAT.BC1_UNORM => RawImageDataCompressedFormat.DXT1,
                    DXGI_FORMAT.BC2_UNORM => RawImageDataCompressedFormat.DXT3,
                    DXGI_FORMAT.BC3_UNORM => RawImageDataCompressedFormat.DXT5,
                    _ => throw new ArgumentException("The image is not block compressed", nameof(metadata.Format))
                };

                // Decompress to BGRA32
                using ScratchImage bgraScratchImg = ConvertAndDecompress(scratchImg, DXGI_FORMAT.B8G8R8A8_UNORM);

                // Get the raw bytes
                byte[] rawData = bgraScratchImg.GetImage(0).GetRawBytes();

                return new RawImageData(compressedData, compressedFormat, rawData, RawImageDataPixelFormat.Bgra32, GetMetadata(metadata));
            }
            else
            {
                // Decompress to BGRA32
                using ScratchImage bgraScratchImg = ConvertAndDecompress(scratchImg, DXGI_FORMAT.B8G8R8A8_UNORM);

                // Get the raw bytes
                byte[] rawData = bgraScratchImg.GetImage(0).GetRawBytes();

                return new RawImageData(rawData, RawImageDataPixelFormat.Bgra32, GetMetadata(metadata));
            }
        }
        finally
        {
            Marshal.FreeHGlobal(imgDataPtr);
        }
    }

    public override ImageMetadata Encode(RawImageData data, Stream outputStream)
    {
        int width = data.Metadata.Width;
        int height = data.Metadata.Height;

        if (!width.IsPowerOfTwo() || !height.IsPowerOfTwo())
            throw new Exception("In order to ensure full compatibility and to generate mipmaps the image must have dimensions which are a power of 2, such as 128, 256, 512, 1024 etc.");

        // If the data is block compressed then we want to maintain that to avoid re-compressing it
        DXGI_FORMAT sourceFormat;
        byte[] sourceData;
        bool shouldCompress;
        if (data.IsBlockCompressed)
        {
            sourceFormat = data.CompressedFormat switch
            {
                RawImageDataCompressedFormat.DXT1 => DXGI_FORMAT.BC1_UNORM,
                RawImageDataCompressedFormat.DXT3 => DXGI_FORMAT.BC2_UNORM,
                RawImageDataCompressedFormat.DXT5 => DXGI_FORMAT.BC3_UNORM,
                _ => throw new ArgumentException("The image is not block compressed", nameof(data.CompressedFormat))
            };
            sourceData = data.CompressedData;
            shouldCompress = false;
        }
        else
        {
            sourceFormat = DXGI_FORMAT.B8G8R8A8_UNORM;
            sourceData = data.PixelFormat switch
            {
                RawImageDataPixelFormat.Bgr24 => data.Convert(RawImageDataPixelFormat.Bgra32),
                RawImageDataPixelFormat.Bgra32 => data.RawData,
                _ => throw new ArgumentOutOfRangeException(nameof(data.PixelFormat), data.PixelFormat, null)
            };
            shouldCompress = true;
        }

        // Computer the row and slice pitch
        TexHelper.Instance.ComputePitch(sourceFormat, width, height, out long rowPitch, out long slicePitch, CP_FLAGS.NONE);
        
        IntPtr rawDataPtr = Marshal.AllocHGlobal((int)slicePitch);
        ScratchImage? scratchImage = null;
        try
        {
            Marshal.Copy(sourceData, 0, rawDataPtr, (int)slicePitch);

            Image img = new(
                width: width,
                height: height,
                format: sourceFormat,
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

            scratchImage = TexHelper.Instance.InitializeTemporary(new[] { img }, texMetadata, null);

            // Generate mipmaps if the format is not compressed
            if (!TexHelper.Instance.IsCompressed(sourceFormat))
            {
                // Separate alpha to avoid alpha blending issues for mipmaps
                const TEX_FILTER_FLAGS filterFlags = TEX_FILTER_FLAGS.BOX | TEX_FILTER_FLAGS.SEPARATE_ALPHA;

                // Generate mipmaps for all level (0 does for all levels down to 1x1)
                ScratchImage mip = scratchImage.GenerateMipMaps(filterFlags, 0);
                scratchImage.Dispose();
                scratchImage = mip;
            }

            // Optionally compress
            if (shouldCompress)
            {
                const TEX_COMPRESS_FLAGS compFlags = TEX_COMPRESS_FLAGS.PARALLEL;

                // Compress to either DXT1 or DXT5
                DXGI_FORMAT format = scratchImage.IsAlphaAllOpaque()
                    ? DXGI_FORMAT.BC1_UNORM  // DXT1
                    : DXGI_FORMAT.BC3_UNORM; // DXT5
                ScratchImage comp = scratchImage.Compress(format, compFlags, 0.5f);
                scratchImage.Dispose();
                scratchImage = comp;
            }

            UnmanagedMemoryStream ddsStream = scratchImage.SaveToDDSMemory(DDS_FLAGS.NONE);
            ddsStream.CopyToEx(outputStream);

            return GetMetadata(scratchImage.GetMetadata());
        }
        finally
        {
            Marshal.FreeHGlobal(rawDataPtr);
            scratchImage?.Dispose();
        }
    }
}