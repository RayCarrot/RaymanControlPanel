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


    public override FileExtension[] FileExtensions { get; } =
    {
        new(".dds"),
    };

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

    // TODO-UPDATE: Don't decompress here!
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
            using ScratchImage bgraScratchImg = ConvertAndDecompress(scratchImg, DXGI_FORMAT.B8G8R8A8_UNORM);

            // Get the primary image
            Image primaryImg = bgraScratchImg.GetImage(0);

            // Get the raw bytes
            byte[] rawBytes = primaryImg.GetRawBytes();

            return new RawImageData(rawBytes, RawImageDataPixelFormat.Bgra32, GetMetadata(scratchImg.GetMetadata()));
        }
        finally
        {
            Marshal.FreeHGlobal(imgDataPtr);
        }
    }

    public override ImageMetadata Encode(RawImageData data, Stream outputStream)
    {
        byte[] rawData = data.PixelFormat switch
        {
            RawImageDataPixelFormat.Bgr24 => data.Convert(RawImageDataPixelFormat.Bgra32),
            RawImageDataPixelFormat.Bgra32 => data.RawData,
            _ => throw new ArgumentOutOfRangeException(nameof(data.PixelFormat), data.PixelFormat, null)
        };

        int width = data.Metadata.Width;
        int height = data.Metadata.Height;

        if (!width.IsPowerOfTwo() || !height.IsPowerOfTwo())
            throw new Exception("In order to ensure full compatibility and to generate mipmaps the image must have dimensions which are a power of 2, such as 128, 256, 512, 1024 etc.");

        int rowPitch = width * 4;
        int slicePitch = width * height * 4;

        IntPtr rawDataPtr = Marshal.AllocHGlobal(slicePitch);
        try
        {
            Marshal.Copy(rawData, 0, rawDataPtr, slicePitch);

            Image img = new(
                width: width,
                height: height,
                format: DXGI_FORMAT.B8G8R8A8_UNORM,
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

            using ScratchImage scratchImage = TexHelper.Instance.InitializeTemporary(new[] { img }, texMetadata, null);

            // Separate alpha to avoid alpha blending issues for mipmaps
            const TEX_FILTER_FLAGS filterFlags = TEX_FILTER_FLAGS.BOX | TEX_FILTER_FLAGS.SEPARATE_ALPHA;
            const TEX_COMPRESS_FLAGS compFlags = TEX_COMPRESS_FLAGS.PARALLEL;

            // Generate mipmaps for all level (0 does for all levels down to 1x1)
            using ScratchImage mip = scratchImage.GenerateMipMaps(filterFlags, 0);
            
            // Compress to either DXT1 or DXT5
            DXGI_FORMAT format = mip.IsAlphaAllOpaque() 
                ? DXGI_FORMAT.BC1_UNORM  // DXT1
                : DXGI_FORMAT.BC3_UNORM; // DXT5
            using ScratchImage comp = mip.Compress(format, compFlags, 0.5f);

            UnmanagedMemoryStream ddsStream = comp.SaveToDDSMemory(DDS_FLAGS.NONE);
            ddsStream.CopyToEx(outputStream);

            return GetMetadata(comp.GetMetadata());
        }
        finally
        {
            Marshal.FreeHGlobal(rawDataPtr);
        }
    }
}