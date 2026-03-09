using System.IO;
using System.Windows.Media.Imaging;
using MahApps.Metro.IconPacks;
using RayCarrot.RCP.Metro.Imaging;

namespace RayCarrot.RCP.Metro.Archive;

/// <summary>
/// A common image file type
/// </summary>
public sealed class ImageFileType : FileType
{
    #region Constructor

    public ImageFileType()
    {
        ImageFormat[] supportedFormats = 
        {
            new PngImageFormat(),
            new JpgImageFormat(),
            new BmpImageFormat(),
            new TgaImageFormat(),
            new DdsImageFormat(),
            new PcxImageFormat(),
        };
        SubTypes = supportedFormats.Select(x => new ImageSubFileType(x, supportedFormats)).ToArray();
    }

    #endregion

    #region Private Properties

    private ImageSubFileType[] SubTypes { get; }

    #endregion

    #region Public Properties

    public override string TypeDisplayName => Resources.Archive_Format_Img;
    public override PackIconMaterialKind Icon => PackIconMaterialKind.ImageOutline;

    #endregion

    #region Private Methods

    private ImageSubFileType GetSubType(FileExtension fileExtension) =>
        SubTypes.First(x => x.ImageFormat.FileExtensions.Contains(fileExtension));

    #endregion

    #region Public Methods

    public override bool IsOfType(FileExtension fileExtension, IArchiveDataManager manager) => 
        SubTypes.Any(x => x.ImageFormat.FileExtensions.Contains(fileExtension));

    public override SubFileType GetSubType(FileExtension fileExtension, ArchiveFileStream inputStream, IArchiveDataManager manager) => 
        GetSubType(fileExtension);

    public override FileThumbnailData LoadThumbnail(
        ArchiveFileStream inputStream, 
        FileExtension fileExtension, 
        IArchiveDataManager manager,
        int requestedWidth,
        int requestedHeight)
    {
        ImageFormat imageFormat = GetSubType(fileExtension).ImageFormat;

        if (imageFormat.CanDecode)
        {
            RawImageData imgData = imageFormat.Decode(inputStream.Stream);
            BitmapSource thumb = imgData.ToBitmapSource(requestedWidth, requestedHeight);

            return new FileThumbnailData(thumb, imgData.GetInfoItems().ToArray());
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
        ImageFormat inputImageFormat = GetSubType(inputFormat).ImageFormat;
        ImageFormat outputImageFormat = GetSubType(outputFormat).ImageFormat;

        // Don't convert if it's the same format
        if (inputImageFormat == outputImageFormat)
        {
            // Copy the image data
            inputStream.Stream.CopyToEx(outputStream);
        }
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
        ImageFormat inputImageFormat = GetSubType(inputFormat).ImageFormat;
        ImageFormat outputImageFormat = GetSubType(outputFormat).ImageFormat;

        // Don't convert if it's the same format
        if (inputImageFormat == outputImageFormat)
        {
            // Copy the image data
            inputStream.Stream.CopyToEx(outputStream.Stream);
        }
        else
        {
            // Convert the image data
            inputImageFormat.Convert(inputStream.Stream, outputStream.Stream, outputImageFormat);
        }
    }

    #endregion

    #region Classes

    private class ImageSubFileType : SubFileType
    {
        public ImageSubFileType(ImageFormat imageFormat, ImageFormat[] supportedFormats)
            : base(imageFormat.Name, GetImportFormats(imageFormat, supportedFormats), GetExportFormats(imageFormat, supportedFormats))
        {
            ImageFormat = imageFormat;
        }

        public ImageFormat ImageFormat { get; }

        private static FileExtension[] GetImportFormats(ImageFormat imageFormat, ImageFormat[] supportedFormats)
        {
            if (imageFormat.CanEncode)
            {
                return supportedFormats.
                    Where(x => x.CanEncode).
                    SelectMany(x => x.FileExtensions).
                    ToArray();
            }
            else
            {
                return Array.Empty<FileExtension>();
            }
        }

        private static FileExtension[] GetExportFormats(ImageFormat imageFormat, ImageFormat[] supportedFormats)
        {
            if (imageFormat.CanDecode)
            {
                return supportedFormats.
                    Where(x => x.CanDecode).
                    SelectMany(x => x.FileExtensions.Take(1)).
                    ToArray();
            }
            else
            {
                return Array.Empty<FileExtension>();
            }
        }
    }

    #endregion
}