using System.IO;
using System.Windows.Media.Imaging;
using BinarySerializer;
using BinarySerializer.OpenSpace;
using ImageMagick;
using MahApps.Metro.IconPacks;
using RayCarrot.RCP.Metro.Imaging;

namespace RayCarrot.RCP.Metro.Archive.CPA;

/// <summary>
/// A texture file type for CPA games
/// </summary>
public sealed class CpaGfFileType : FileType
{
    #region Constructor

    public CpaGfFileType()
    {
        SupportedFormats = new ImageFormat[]
        {
            new PngImageFormat(),
            new JpgImageFormat(),
            new BmpImageFormat(),
            new TgaImageFormat(OrientationType.BottomLeft), // .gf textures should be oriented to bottom-left (upside-down)
        };

        // Could create a separate subtype per image format, but that seems silly. Better to just have a global one.
        GfSubType = new CpaGfSubFileType(SupportedFormats);
    }

    #endregion

    #region Private Properties

    private ImageFormat[] SupportedFormats { get; }
    private CpaGfSubFileType GfSubType { get; }

    #endregion

    #region Public Properties

    public override string TypeDisplayName => Resources.Archive_Format_GF;
    public override PackIconMaterialKind Icon => PackIconMaterialKind.ImageOutline;

    #endregion

    #region Private Methods

    private ImageFormat GetImageFormat(FileExtension fileExtension) =>
        SupportedFormats.First(x => x.FileExtensions.Contains(fileExtension));

    private CpaGfImageFormat GetImageFormat(IArchiveDataManager manager) => 
        new(manager.Context!.GetRequiredSettings<OpenSpaceSettings>());

    private GFFileHeader ReadFileHeader(ArchiveFileStream fileStream, IArchiveDataManager manager)
    {
        return manager.Context!.ReadStreamData<GFFileHeader>(fileStream.Stream, name: fileStream.Name, mode: VirtualFileMode.DoNotClose);
    }

    #endregion

    #region Public Methods

    public override bool IsSupported(IArchiveDataManager manager) => manager.Context?.HasSettings<OpenSpaceSettings>() is true;

    public override bool IsOfType(FileExtension fileExtension, IArchiveDataManager manager) => fileExtension.PrimaryFileExtension == ".gf";

    public override SubFileType GetSubType(FileExtension fileExtension, ArchiveFileStream inputStream, IArchiveDataManager manager) => GfSubType;

    public override FileThumbnailData LoadThumbnail(
        ArchiveFileStream inputStream, 
        FileExtension fileExtension, 
        IArchiveDataManager manager,
        int requestedWidth,
        int requestedHeight)
    {
        CpaGfImageFormat imageFormat = GetImageFormat(manager);
        RawImageData imgData = imageFormat.Decode(inputStream.Stream);
        BitmapSource thumb = imgData.ToBitmapSource(requestedWidth, requestedHeight);

        return new FileThumbnailData(thumb, imgData.GetInfoItems().ToArray());
    }

    public override void ConvertTo(
        FileExtension inputFormat, 
        FileExtension outputFormat, 
        ArchiveFileStream inputStream, 
        Stream outputStream, 
        IArchiveDataManager manager)
    {
        // Get the formats
        CpaGfImageFormat inputImageFormat = GetImageFormat(manager);
        ImageFormat outputImageFormat = GetImageFormat(outputFormat);

        // Convert the image data
        inputImageFormat.Convert(inputStream.Stream, outputStream, outputImageFormat);
    }

    public override void ConvertFrom(
        FileExtension inputFormat, 
        FileExtension outputFormat, 
        ArchiveFileStream currentFileStream, 
        ArchiveFileStream inputStream,
        ArchiveFileStream outputStream, 
        IArchiveDataManager manager)
    {
        // Get the formats
        ImageFormat inputImageFormat = GetImageFormat(inputFormat);
        CpaGfImageFormat outputImageFormat = GetImageFormat(manager);

        // Decode the file to convert from
        RawImageData decodedData = inputImageFormat.Decode(inputStream.Stream);

        // Read the original file header
        GFFileHeader originalGfHeader = ReadFileHeader(currentFileStream, manager);
        GF_Format originalFormat = originalGfHeader.PixelFormat;

        // Optionally force the new pixel format to be 888 or 8888, depending on if the alpha channel is used
        if (Services.Data.Archive_GF_ForceGF8888Import)
            originalFormat = originalFormat switch
            {
                GF_Format.BGRA_8888 => GF_Format.BGRA_8888,
                GF_Format.BGR_888 => GF_Format.BGR_888,
                GF_Format.GrayscaleAlpha_88 => GF_Format.BGRA_8888,
                GF_Format.BGRA_4444 => GF_Format.BGRA_8888,
                GF_Format.BGRA_1555 => GF_Format.BGRA_8888,
                GF_Format.BGR_565 => GF_Format.BGR_888,
                GF_Format.BGRA_Indexed => GF_Format.BGRA_8888,
                GF_Format.BGR_Indexed => GF_Format.BGR_888,
                GF_Format.Grayscale => GF_Format.BGR_888,
                _ => throw new ArgumentOutOfRangeException()
            };

        // Check if the format should be updated for transparency
        if (Services.Data.Archive_GF_UpdateTransparency != GFTransparencyMode.PreserveFormat)
        {
            // Check if the imported file is transparent
            bool isTransparent = decodedData.PixelFormat switch 
            {
                RawImageDataPixelFormat.Bgr24 => false,
                RawImageDataPixelFormat.Bgra32 => Services.Data.Archive_GF_UpdateTransparency == GFTransparencyMode.UpdateBasedOnPixelFormat ||
                                                  decodedData.UtilizesAlpha(),
                _ => throw new ArgumentOutOfRangeException()
            };

            // NOTE: Currently only supported for formats with 3 or 4 channels
            // Check if the format should be updated for transparency
            if (originalGfHeader.BytesPerPixel >= 3)
                // Update the format
                originalFormat = isTransparent ? GF_Format.BGRA_8888 : GF_Format.BGR_888;
        }

        // Convert
        outputImageFormat.Encode(decodedData, outputStream.Stream, Services.Data.Archive_GF_GenerateMipmaps, originalFormat);
    }

    #endregion

    #region Classes

    private class CpaGfSubFileType : SubFileType
    {
        public CpaGfSubFileType(ImageFormat[] supportedFormats)
            : base(GetImportFormats(supportedFormats), GetExportFormats(supportedFormats))
        { }

        private static FileExtension[] GetImportFormats(ImageFormat[] supportedFormats)
        {
            return supportedFormats.
                Where(x => x.CanEncode).
                SelectMany(x => x.FileExtensions).
                ToArray();
        }

        private static FileExtension[] GetExportFormats(ImageFormat[] supportedFormats)
        {
            return supportedFormats.
                Where(x => x.CanDecode).
                SelectMany(x => x.FileExtensions.Take(1)).
                ToArray();
        }
    }

    #endregion
}