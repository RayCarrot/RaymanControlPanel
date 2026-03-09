using System.IO;
using System.Windows.Media.Imaging;
using BinarySerializer;
using BinarySerializer.UbiArt;
using MahApps.Metro.IconPacks;
using RayCarrot.RCP.Metro.Imaging;

namespace RayCarrot.RCP.Metro.Archive.UbiArt;

/// <summary>
/// A cooked UbiArt texture file type
/// </summary>
public sealed class CookedUbiArtTextureFileType : FileType
{
    #region Constructor

    public CookedUbiArtTextureFileType()
    {
        ImageFormat[] defaultFormats =
        [
            new PngImageFormat(),
            new JpgImageFormat(),
            new BmpImageFormat(),
            new TgaImageFormat(),
            new DdsImageFormat()
        ];

        DdsSubType = new CookedUbiArtTextureSubFileType(new DdsImageFormat(), defaultFormats);
        Xbox360D3DTextureSubType = new CookedUbiArtTextureSubFileType(new Xbox360D3DTextureImageFormat(), defaultFormats);
        GtfImageFormat = new CookedUbiArtTextureSubFileType(new GtfImageFormat(), defaultFormats);
        PvrSubType = new CookedUbiArtTextureSubFileType(new PvrImageFormat(), defaultFormats);
    }

    #endregion

    #region Private Properties

    private CookedUbiArtTextureSubFileType DdsSubType { get; }
    private CookedUbiArtTextureSubFileType Xbox360D3DTextureSubType { get; }
    private CookedUbiArtTextureSubFileType GtfImageFormat { get; }
    private CookedUbiArtTextureSubFileType PvrSubType { get; }

    #endregion

    #region Public Properties

    public override string TypeDisplayName => Resources.Archive_Format_CookedUbiArtTexture;
    public override PackIconMaterialKind Icon => PackIconMaterialKind.ImageOutline;

    #endregion

    #region Private Methods

    private void RemapChannels(RawImageData rawImageData, TextureCooked header, int mipmapLevel)
    {
        byte[] imgData = rawImageData.GetImageData(mipmapLevel);
        byte[] colors = new byte[6];
        colors[0] = 0xFF;
        colors[4] = 0x00;
        colors[5] = 0xFF;

        switch (rawImageData.PixelFormat)
        {
            case RawImageDataPixelFormat.Bgr24:
                for (int i = 0; i < imgData.Length; i += 3)
                {
                    colors[1] = imgData[i + 2]; // Red
                    colors[2] = imgData[i + 1]; // Green
                    colors[3] = imgData[i + 0]; // Blue

                    imgData[i + 2] = colors[header.Remap_R];
                    imgData[i + 1] = colors[header.Remap_G];
                    imgData[i + 0] = colors[header.Remap_B];
                }
                break;
            
            case RawImageDataPixelFormat.Bgra32:
                for (int i = 0; i < imgData.Length; i += 4)
                {
                    colors[0] = imgData[i + 3]; // Alpha
                    colors[1] = imgData[i + 2]; // Red
                    colors[2] = imgData[i + 1]; // Green
                    colors[3] = imgData[i + 0]; // Blue

                    imgData[i + 3] = colors[header.Remap_A];
                    imgData[i + 2] = colors[header.Remap_R];
                    imgData[i + 1] = colors[header.Remap_G];
                    imgData[i + 0] = colors[header.Remap_B];
                }
                break;

            default:
                throw new ArgumentOutOfRangeException(nameof(rawImageData.PixelFormat), rawImageData.PixelFormat, null);
        }
    }

    private TextureCooked? ReadCookedHeader(ArchiveFileStream inputStream, UbiArtSettings settings, IArchiveDataManager manager)
    {
        if (!HasCookedHeader(settings))
            return null;

        // Serialize the header
        return manager.Context!.ReadStreamData<TextureCooked>(
            stream: inputStream.Stream,
            name: inputStream.Name,
            mode: VirtualFileMode.DoNotClose,
            onPreSerialize: x => x.Pre_SerializeRawData = false);
    }

    private void WriteCookedHeader(TextureCooked header, ArchiveFileStream outputStream, UbiArtSettings settings, IArchiveDataManager manager)
    {
        if (!HasCookedHeader(settings))
            return;

        // Serialize the header
        header.Pre_SerializeRawData = false;
        manager.Context!.WriteStreamData<TextureCooked>(
            stream: outputStream.Stream,
            obj: header,
            name: outputStream.Name,
            mode: VirtualFileMode.DoNotClose);
    }

    private bool HasCookedHeader(UbiArtSettings settings)
    {
        return settings.Game != BinarySerializer.UbiArt.Game.RaymanOrigins;
    }

    private bool IsSupported(UbiArtSettings settings)
    {
        return settings.Platform is 
            Platform.PC or 
            Platform.Xbox360 or 
            Platform.PlayStation3 or 
            Platform.iOS or 
            Platform.Android or
            Platform.Mac;
    }

    private CookedUbiArtTextureSubFileType GetSubType(UbiArtSettings settings, Func<TextureCooked?> getHeaderFunc)
    {
        return settings.Platform switch
        {
            Platform.PC => DdsSubType,
            Platform.Xbox360 => Xbox360D3DTextureSubType,
            Platform.PlayStation3 => GtfImageFormat,
            Platform.iOS => getHeaderFunc()?.Type is TextureType.BackLightEmmissive or (TextureType)3 ? PvrSubType : DdsSubType,
            Platform.Android => DdsSubType,
            Platform.Mac => DdsSubType,
            _ => throw new InvalidOperationException("Textures are not supported for the current platform")
        };
    }

    // TODO-UPDATE: Issue with multiple extensions. If you import with multiple extensions (such as "File.tga.ckd.png") then this fails.
    private ImageFormat GetImageFormat(CookedUbiArtTextureSubFileType subType, FileExtension fileExtension) =>
        subType.SupportedFormats.First(x => x.FileExtensions.Contains(fileExtension));

    #endregion

    #region Public Methods

    public override bool IsSupported(IArchiveDataManager manager) => manager.Context?.HasSettings<UbiArtSettings>() is true;

    public override bool IsOfType(FileExtension fileExtension, IArchiveDataManager manager) =>
        IsSupported(manager.Context!.GetRequiredSettings<UbiArtSettings>()) &&
        (fileExtension == new FileExtension(".tga.ckd", multiple: true) || 
         fileExtension == new FileExtension(".png.ckd", multiple: true));

    public override SubFileType GetSubType(FileExtension fileExtension, ArchiveFileStream inputStream, IArchiveDataManager manager)
    {
        // Get the settings
        UbiArtSettings settings = manager.Context!.GetRequiredSettings<UbiArtSettings>();

        // Return the subtype
        return GetSubType(settings, () => ReadCookedHeader(inputStream, settings, manager));
    }

    public override FileThumbnailData LoadThumbnail(
        ArchiveFileStream inputStream, 
        FileExtension fileExtension, 
        IArchiveDataManager manager)
    {
        // Get the settings
        UbiArtSettings settings = manager.Context!.GetRequiredSettings<UbiArtSettings>();

        // Read the header
        TextureCooked? header = ReadCookedHeader(inputStream, settings, manager);

        // Decode the image
        ImageFormat imageFormat = GetSubType(settings, () => header).ImageFormat;

        if (imageFormat.CanDecode)
        {
            RawImageData imgData = imageFormat.Decode(inputStream.Stream);

            // Remap if needed
            if (header is { IsRemapped: true })
                RemapChannels(imgData, header, 0);

            // Create an image source
            BitmapSource thumb = imgData.ToBitmapSource();

            return new FileThumbnailData(thumb, imgData.Metadata.GetInfoItems().ToArray());
        }
        else
        {
            return new FileThumbnailData(null, Array.Empty<DuoGridItemViewModel>());
        }
    }

    public override void ConvertTo(
        FileExtension inputFormat, 
        FileExtension outputFormat, 
        ArchiveFileStream inputStream, 
        Stream outputStream,
        IArchiveDataManager manager)
    {
        // Get the settings
        UbiArtSettings settings = manager.Context!.GetRequiredSettings<UbiArtSettings>();

        // Read the header
        TextureCooked? header = ReadCookedHeader(inputStream, settings, manager);

        // Get the sub-type
        CookedUbiArtTextureSubFileType subType = GetSubType(settings, () => header);

        // Get the formats
        ImageFormat inputImageFormat = subType.ImageFormat;
        ImageFormat outputImageFormat = GetImageFormat(subType, outputFormat);

        // Convert manually if remapped
        if (header is { IsRemapped: true })
        {
            RawImageData decodedData = inputImageFormat.Decode(inputStream.Stream);

            // Remove the compressed data since we can only remap the raw data
            decodedData = decodedData.WithoutCompressedData();
            
            // Remap each mipmap level
            for (int i = 0; i < decodedData.MipmapLevels; i++)
                RemapChannels(decodedData, header, i);
            
            outputImageFormat.Encode(decodedData, outputStream);
        }
        // Don't convert if it's the same format. For example if exporting as .dds on PC.
        else if (inputImageFormat == outputImageFormat)
        {
            // Copy the image data
            inputStream.Stream.CopyToEx(outputStream);
        }
        // Convert
        else
        {
            // Convert the image data
            inputImageFormat.Convert(inputStream.Stream, outputStream, outputImageFormat);
        }
    }

    public override void ConvertFrom(
        FileExtension inputFormat, 
        FileExtension outputFormat, 
        ArchiveFileStream currentFileStream,
        ArchiveFileStream inputStream, 
        ArchiveFileStream outputStream,
        IArchiveDataManager manager)
    {
        // Get the settings
        UbiArtSettings settings = manager.Context!.GetRequiredSettings<UbiArtSettings>();

        // Read the header
        TextureCooked? header = ReadCookedHeader(currentFileStream, settings, manager);

        // Get the sub-type
        CookedUbiArtTextureSubFileType subType = GetSubType(settings, () => header);

        // Get the formats
        ImageFormat inputImageFormat = GetImageFormat(subType, inputFormat);
        ImageFormat outputImageFormat = subType.ImageFormat;

        // Skip the header since we right that last
        int dataOffset = (int)(header?.RawDataStartOffset ?? 0);
        outputStream.Stream.Position = dataOffset;

        ImageMetadata? metadata = null;

        // Don't convert if it's the same format
        if (inputImageFormat == outputImageFormat)
        {
            // Copy the image data
            inputStream.Stream.CopyToEx(outputStream.Stream);
        }
        else
        {
            // Convert the image data
            metadata = inputImageFormat.Convert(inputStream.Stream, outputStream.Stream, outputImageFormat);
        }

        // Write the header if there is one
        if (header != null)
        {
            // Get the metadata if it's null
            outputStream.Stream.Position = dataOffset;
            metadata ??= outputImageFormat.GetMetadata(outputStream.Stream);

            uint dataSize = (uint)(outputStream.Stream.Length - dataOffset);

            header.Width = (ushort)metadata.Width;
            header.Height = (ushort)metadata.Height;
            header.RawDataSize = dataSize;
            header.MemorySize = dataSize;
            header.Remap = 0x00010203;

            outputStream.Stream.Position = 0;
            WriteCookedHeader(header, outputStream, settings, manager);
        }
    }

    #endregion

    #region Classes

    private class CookedUbiArtTextureSubFileType : SubFileType
    {
        public CookedUbiArtTextureSubFileType(ImageFormat imageFormat, ImageFormat[] supportedFormats)
            : base(imageFormat.Name, GetImportFormats(imageFormat, supportedFormats), GetExportFormats(imageFormat, supportedFormats))
        {
            ImageFormat = imageFormat;
            SupportedFormats = supportedFormats.Append(imageFormat).Distinct().ToArray();
        }

        public ImageFormat[] SupportedFormats { get; }
        public ImageFormat ImageFormat { get; }

        private static FileExtension[] GetImportFormats(ImageFormat imageFormat, ImageFormat[] supportedFormats)
        {
            // If the texture can be encoded then we have a lot of formats to choose between...
            if (imageFormat.CanEncode)
            {
                List<FileExtension> extensions = new();

                // Allow importing from the same format if we can decode it (we need to decode to update the cooked header)
                if (imageFormat.CanDecode)
                    extensions.AddRange(imageFormat.FileExtensions);

                // Add the supported formats
                extensions.AddRange(supportedFormats.Where(x => x.CanEncode).SelectMany(x => x.FileExtensions));
                
                // Return distinct ones (since supported formats might contain the main format)
                return extensions.Distinct().ToArray();
            }
            else
            {
                // If we can't encode, but we can decode, then we can import the same format (we need to decode to update the cooked header)
                if (imageFormat.CanDecode)
                    return imageFormat.FileExtensions;
                else
                    return Array.Empty<FileExtension>();
            }
        }

        private static FileExtension[] GetExportFormats(ImageFormat imageFormat, ImageFormat[] supportedFormats)
        {
            // If the texture can be decoded then we have a lot of formats to choose between...
            if (imageFormat.CanDecode)
            {
                List<FileExtension> extensions = new();

                // Allow exporting from the same format
                extensions.AddRange(imageFormat.FileExtensions.Take(1));

                // Add the supported formats
                extensions.AddRange(supportedFormats.Where(x => x.CanEncode).SelectMany(x => x.FileExtensions.Take(1)));

                // Return distinct ones (since supported formats might contain the main format)
                return extensions.Distinct().ToArray();
            }
            else
            {
                // If we can't decode then we can always export as the main format itself
                return imageFormat.FileExtensions.Take(1).ToArray();
            }
        }
    }

    #endregion
}