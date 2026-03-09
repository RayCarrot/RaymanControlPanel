using System.IO;
using MahApps.Metro.IconPacks;

namespace RayCarrot.RCP.Metro.Archive;

/// <summary>
/// Handles data for a specific file type
/// </summary>
public abstract class FileType
{
    /// <summary>
    /// The display name for the file type
    /// </summary>
    public abstract string TypeDisplayName { get; }

    /// <summary>
    /// The default icon kind for the type
    /// </summary>
    public abstract PackIconMaterialKind Icon { get; }

    /// <summary>
    /// Indicates if the specified manager supports files of this type
    /// </summary>
    /// <param name="manager">The manager to check</param>
    /// <returns>True if supported, otherwise false</returns>
    public virtual bool IsSupported(IArchiveDataManager manager) => true;

    /// <summary>
    /// Indicates if a file with the specifies file extension is of this type
    /// </summary>
    /// <param name="fileExtension">The file extension to check</param>
    /// <param name="manager">The manager</param>
    /// <returns>True if it is of this type, otherwise false</returns>
    public virtual bool IsOfType(FileExtension fileExtension, IArchiveDataManager manager) => false;

    /// <summary>
    /// Indicates if a file with the specifies file extension and data is of this type
    /// </summary>
    /// <param name="fileExtension">The file extension to check</param>
    /// <param name="inputStream">The file data to check</param>
    /// <param name="manager">The manager</param>
    /// <returns>True if it is of this type, otherwise false</returns>
    public virtual bool IsOfType(
        FileExtension fileExtension, 
        ArchiveFileStream inputStream, 
        IArchiveDataManager manager) 
        => false;

    /// <summary>
    /// Gets the supported formats to import from
    /// </summary>
    public virtual SubFileType GetSubType(FileExtension fileExtension, ArchiveFileStream inputStream, IArchiveDataManager manager) => new();

    /// <summary>
    /// Loads the thumbnail and display info for the file
    /// </summary>
    /// <param name="inputStream">The file data stream</param>
    /// <param name="fileExtension">The file extension</param>
    /// <param name="manager">The manager</param>
    /// <param name="requestedWidth">The requested width</param>
    /// <param name="requestedHeight">The requested height</param>
    /// <returns>The thumbnail data</returns>
    public virtual FileThumbnailData LoadThumbnail(
        ArchiveFileStream inputStream, 
        FileExtension fileExtension, 
        IArchiveDataManager manager,
        int requestedWidth,
        int requestedHeight) 
        => new(null, Array.Empty<DuoGridItemViewModel>());

    /// <summary>
    /// Converts the file data to the specified format
    /// </summary>
    /// <param name="inputFormat">The format to convert from</param>
    /// <param name="outputFormat">The format to convert to</param>
    /// <param name="inputStream">The input file data stream</param>
    /// <param name="outputStream">The output stream for the converted data</param>
    /// <param name="manager">The manager</param>
    public abstract void ConvertTo(
        FileExtension inputFormat, 
        FileExtension outputFormat, 
        ArchiveFileStream inputStream, 
        Stream outputStream, 
        IArchiveDataManager manager);

    /// <summary>
    /// Converts the file data from the specified format
    /// </summary>
    /// <param name="inputFormat">The format to convert from</param>
    /// <param name="outputFormat">The format to convert to</param>
    /// <param name="currentFileStream">The current file stream</param>
    /// <param name="inputStream">The input file data stream to convert from</param>
    /// <param name="outputStream">The output stream for the converted data</param>
    /// <param name="manager">The manager</param>
    public abstract void ConvertFrom(
        FileExtension inputFormat, 
        FileExtension outputFormat, 
        ArchiveFileStream currentFileStream, 
        ArchiveFileStream inputStream, 
        ArchiveFileStream outputStream, 
        IArchiveDataManager manager);
}