using System.Diagnostics.CodeAnalysis;
using System.IO;
using RayCarrot.RCP.Metro.Archive.Bakesale;
using RayCarrot.RCP.Metro.Archive.CPA;
using RayCarrot.RCP.Metro.Archive.UbiArt;

namespace RayCarrot.RCP.Metro.Archive;

/// <summary>
/// File item data for the Archive Explorer
/// </summary>
public class FileItem : IDisposable
{
    static FileItem()
    {
        // Set the supported file types
        FileTypes = new FileType[]
        {
            new CpaGfFileType(),
            new WaveSoundFileType(),
            new CookedUbiArtSoundFileType(),
            new ImageFileType(),
            new CookedUbiArtTextureFileType(),
            new BakesaleSpritesFileType(),
            new BakesaleWavesFileType(),
        };

        // Set default file type
        DefaultFileType = new DefaultFileType();
    }

    public FileItem(IArchiveDataManager manager, string fileName, string directory, object archiveEntry)
    {
        Manager = manager;
        FileName = fileName;
        Directory = directory;
        ArchiveEntry = archiveEntry;
    }

    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    protected IArchiveDataManager Manager { get; }

    public string FileName { get; }
    public string Directory { get; }
    public object ArchiveEntry { get; }

    public TempFileStream? PendingImport { get; protected set; }

    [MemberNotNullWhen(true, nameof(PendingImport))]
    public bool IsPendingImport => PendingImport != null;

    public FileExtension FileExtension => new FileExtension(FileName, multiple: true);

    public ArchiveFileStream GetFileData(IDisposable? generator)
    {
        if (!IsPendingImport && generator == null)
            throw new ArgumentNullException(nameof(generator), "A generator must be specified if there is no pending import");

        // Get the stream
        ArchiveFileStream stream = IsPendingImport 
            ? new ArchiveFileStream(PendingImport, FileName, false) 
            : new ArchiveFileStream(() => Manager.GetFileData(generator!, ArchiveEntry), FileName, true);
            
        // Seek to the beginning
        stream.SeekToBeginning();

        // Return the stream
        return stream;
    }

    public ArchiveFileStream GetDecodedFileData(IDisposable? generator)
    {
        ArchiveFileStream? encodedStream = null;
        ArchiveFileStream? decodedStream = null;

        try
        {
            // Get the encoded file bytes
            encodedStream = GetFileData(generator);

            // Create a stream for the decoded bytes
            decodedStream = new ArchiveFileStream(new MemoryStream(), FileName, true);

            // Decode the bytes
            Manager.DecodeFile(encodedStream.Stream, decodedStream.Stream, ArchiveEntry);

            // Check if the data was decoded
            if (decodedStream.Stream.Length > 0)
            {
                encodedStream.Dispose();
                decodedStream.SeekToBeginning();
                return decodedStream;
            }
            else
            {
                decodedStream.Dispose();
                encodedStream.SeekToBeginning();
                return encodedStream;
            }
        }
        catch (Exception ex)
        {
            // Dispose both streams if an exception is thrown
            encodedStream?.Dispose();
            decodedStream?.Dispose();

            Logger.Error(ex, "Getting decoded archive file data");

            throw;
        }
    }

    [MemberNotNull(nameof(PendingImport))]
    public void SetPendingImport()
    {
        PendingImport?.Dispose();
        PendingImport = TempFileStream.Create();
    }

    public FileType GetFileType(ArchiveFileStream stream)
    {
        // Get types supported by the current manager
        FileType[] types = FileTypes.Where(x => x.IsSupported(Manager)).ToArray();

        // First attempt to find matching file type based off of the file extension to avoid having to read the file
        FileType? match = types.FirstOrDefault(x => x.IsOfType(FileExtension, Manager));

        // If no match, check the data
        if (match == null)
        {
            // Find a match from the stream data
            match = types.FirstOrDefault(x => x.IsOfType(FileExtension, stream, Manager));
        }

        // Return the type and set to default if still null
        return match ?? DefaultFileType;
    }

    private static FileType[] FileTypes { get; }
    private static FileType DefaultFileType { get; }

    public void Dispose()
    {
        PendingImport?.Dispose();
    }
}