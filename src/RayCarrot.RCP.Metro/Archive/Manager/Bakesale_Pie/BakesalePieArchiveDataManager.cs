using System.IO;
using System.IO.Compression;
using BinarySerializer;

namespace RayCarrot.RCP.Metro.Archive.Bakesale;

/// <summary>
/// Archive data manager for a Bakesale .pie file
/// </summary>
public class BakesalePieArchiveDataManager : IArchiveDataManager
{
    #region Constructor

    public BakesalePieArchiveDataManager(uint gameKey)
    {
        GameKey = gameKey;
    }

    #endregion

    #region Logger

    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    #endregion

    #region Public Properties

    public uint GameKey { get; }
    public Context? Context => null;
    public char PathSeparatorCharacter => '/';
    public bool CanModifyDirectories => true;
    public FileExtension ArchiveFileExtension => new(".pie");
    public string DefaultArchiveFileName => "assets.pie";
    public object? GetCreatorUIConfig => null;

    #endregion

    #region Public Methods

    public void EncodeFile(Stream inputStream, Stream outputStream, object fileEntry, FileMetadata fileMetadata)
    {
        // Get the file entry
        PieFileEntry entry = (PieFileEntry)fileEntry;

        // Set the file size
        entry.FileSize = inputStream.Length;

        // Set the write time
        if (fileMetadata.LastModified != null)
            entry.LastWriteTime = fileMetadata.LastModified.Value;
    }

    public void DecodeFile(Stream inputStream, Stream outputStream, object fileEntry) { }

    public FileMetadata GetFileMetadata(object fileEntry)
    {
        // Get the file entry
        PieFileEntry entry = (PieFileEntry)fileEntry;

        return new FileMetadata
        {
            LastModified = entry.LastWriteTime
        };
    }

    public Stream GetFileData(IDisposable generator, object fileEntry) => 
        generator.CastTo<IFileGenerator<PieFileEntry>>().GetFileStream((PieFileEntry)fileEntry);

    public ArchiveRepackResult WriteArchive(
        IDisposable? generator,
        object archive,
        ArchiveFileStream outputFileStream,
        IEnumerable<FileItem> files,
        ILoadState loadState)
    {
        Logger.Info("A PIE archive is being repacked...");

        // Create a new zip file
        using ZipArchive mainZipArchive = new(new EncryptedPieStream(outputFileStream.Stream, GameKey), ZipArchiveMode.Create, leaveOpen: true);

        // Keep track of the ifs files and handle those last
        Dictionary<string, List<(FileItem File, string FilePath)>> ifsFiles = new();

        // Add the files
        FileItem[] filesArray = files.ToArray();
        int maxProgress = filesArray.Length;
        int progress = 0;
        foreach (FileItem file in filesArray)
        {
            loadState.CancellationToken.ThrowIfCancellationRequested();

            // If it's within an ifs folder then we store it for later
            if (file.Directory.EndsWith(".ifs", StringComparison.InvariantCulture) || 
                file.Directory.Contains($".ifs{PathSeparatorCharacter}", StringComparison.InvariantCulture))
            {
                string ifsFilePath;
                string filePath;
                if (file.Directory.EndsWith(".ifs"))
                {
                    ifsFilePath = file.Directory;
                    filePath = file.FileName;
                }
                else
                {
                    int ifsIndex = file.Directory.IndexOf($".ifs{PathSeparatorCharacter}", StringComparison.InvariantCulture);
                    ifsFilePath = file.Directory[..(ifsIndex + 4)];
                    filePath = file.Directory[(ifsIndex + 5)..] + PathSeparatorCharacter + file.FileName;
                }

                if (!ifsFiles.TryGetValue(ifsFilePath, out List<(FileItem, string)> ifsFilesList))
                {
                    ifsFilesList = [];
                    ifsFiles.Add(ifsFilePath, ifsFilesList);
                }
                ifsFilesList.Add((file, filePath));
            }
            else
            {
                // Get the file path within the zip
                string filePath;
                if (file.Directory != String.Empty)
                    filePath = file.Directory + PathSeparatorCharacter + file.FileName;
                else
                    filePath = file.FileName;

                // Create a new file entry
                ZipArchiveEntry entry = mainZipArchive.CreateEntry(filePath, CompressionLevel.NoCompression);
                entry.LastWriteTime = ((PieFileEntry)file.ArchiveEntry).LastWriteTime;

                // Copy over the file data
                using Stream newEntryStream = entry.Open();
                using Stream originalFileStream = file.GetFileData(generator).Stream;
                originalFileStream.CopyToEx(newEntryStream);

                progress++;
            }

            // Update progress
            loadState.SetProgress(new Progress(progress, maxProgress));
        }

        // Add the ifs zips
        foreach (string ifsFilePath in ifsFiles.Keys)
        {
            // Get the files to add to the ifs zip
            List<(FileItem File, string FilePath)> ifsFilesList = ifsFiles[ifsFilePath];

            // Create a temp file for packing the ifs zip
            using TempFile tempFile = new(false);
            using FileStream tempFileStream = File.Create(tempFile.TempPath);

            // Add the files to the ifs zip
            using (ZipArchive ifsZipArchive = new(tempFileStream, ZipArchiveMode.Create, leaveOpen: true))
            {
                foreach ((FileItem File, string FilePath) fileValue in ifsFilesList)
                {
                    // Create a new file entry
                    ZipArchiveEntry entry = ifsZipArchive.CreateEntry(fileValue.FilePath, CompressionLevel.Optimal);
                    entry.LastWriteTime = ((PieFileEntry)fileValue.File.ArchiveEntry).LastWriteTime;

                    // Copy over the file data
                    using Stream newEntryStream = entry.Open();
                    using Stream originalFileStream = fileValue.File.GetFileData(generator).Stream;
                    originalFileStream.CopyToEx(newEntryStream);

                    progress++;

                    // Update progress
                    loadState.SetProgress(new Progress(progress, maxProgress));
                }
            }

            tempFileStream.Position = 0;

            // Create a new file entry to the main zip for the ifs zip
            ZipArchiveEntry ifsZipEntry = mainZipArchive.CreateEntry(ifsFilePath, CompressionLevel.NoCompression);
            
            // Copy over the file data
            using Stream newIfsZipEntryStream = ifsZipEntry.Open();
            tempFileStream.CopyToEx(newIfsZipEntryStream);
        }

        Logger.Info("The PIE archive has been repacked");

        return new ArchiveRepackResult();
    }

    public double GetOnRepackedArchivesProgressLength() => 0;

    public Task OnRepackedArchivesAsync(
        FileSystemPath[] archiveFilePaths,
        IReadOnlyList<ArchiveRepackResult> repackResults,
        ILoadState loadState) => Task.CompletedTask;

    public ArchiveData LoadArchiveData(object archive, Stream archiveFileStream, string fileName)
    {
        // Get the data
        PieArchiveData data = (PieArchiveData)archive;

        if (data.MainZipArchive == null)
            throw new Exception("The main zip has not been loaded");

        Logger.Info("The directories are being retrieved for a PIE archive");

        // Get the files
        Dictionary<string, List<FileItem>> dirs = new();
        foreach (ZipArchiveEntry entry in data.MainZipArchive.Entries)
        {
            if (entry.Name != String.Empty)
            {
                // The dreamm.ifs file is another zip, so we unzip it and treat it as a directory
                if (entry.Name.EndsWith(".ifs", StringComparison.InvariantCulture))
                {
                    // Open as a new archive
                    ZipArchive ifsArchive = new(entry.Open(), ZipArchiveMode.Read);
                    data.SubArchives.Add(ifsArchive);

                    // Add files relative to the archive
                    string basePath = entry.FullName;
                    foreach (ZipArchiveEntry ifsEntry in ifsArchive.Entries)
                    {
                        if (ifsEntry.Name != String.Empty)
                            addFile(basePath, ifsEntry);
                    }
                }
                else
                {
                    addFile(null, entry);
                }
            }
        }

        // Helper for adding a file
        void addFile(string? basePath, ZipArchiveEntry entry)
        {
            // Get the directory path
            int lastSeparatorIndex = entry.FullName.LastIndexOf(PathSeparatorCharacter);
            string dir = lastSeparatorIndex != -1
                ? entry.FullName[..lastSeparatorIndex]
                : String.Empty;

            // Combine if needed
            if (basePath != null)
                dir = basePath + PathSeparatorCharacter + dir;

            if (!dirs.TryGetValue(dir, out List<FileItem> list))
            {
                list = new List<FileItem>();
                dirs[dir] = list;
            }

            list.Add(new FileItem(this, entry.Name, dir, new PieFileEntry(entry)));
        }

        // Return the data
        return new ArchiveData(dirs.Select(x => new ArchiveDirectory(x.Key, x.Value.ToArray())), new PieFileGenerator());
    }

    public object LoadArchive(Stream archiveFileStream, string name)
    {
        // Set the stream position to 0
        archiveFileStream.Position = 0;

        // Open the zip archive
        ZipArchive zipArchive = new(new EncryptedPieStream(archiveFileStream, GameKey), ZipArchiveMode.Read, leaveOpen: true);

        Logger.Info("Read PIE file with {0} entries", zipArchive.Entries.Count);

        return new PieArchiveData(zipArchive);
    }

    public object CreateArchive()
    {
        return new PieArchiveData(null);
    }

    public IEnumerable<DuoGridItemViewModel> GetFileInfo(object archive, object fileEntry)
    {
        PieFileEntry entry = (PieFileEntry)fileEntry;

        yield return new DuoGridItemViewModel(
            header: new ResourceLocString(nameof(Resources.Archive_FileInfo_WriteTime)),
            text: $"{entry.LastWriteTime.DateTime.ToLongDateString()}");

        yield return new DuoGridItemViewModel(
            header: new ResourceLocString(nameof(Resources.Archive_FileInfo_Size)),
            text: BinaryHelpers.BytesToString(entry.FileSize));
    }

    public object GetNewFileEntry(object archive, string directory, string fileName)
    {
        return new PieFileEntry(null);
    }

    public long? GetFileSize(object fileEntry, bool encoded)
    {
        PieFileEntry entry = (PieFileEntry)fileEntry;
        return entry.FileSize;
    }

    public void Dispose() { }

    #endregion

    #region Classes

    private class PieFileGenerator : IFileGenerator<PieFileEntry>
    {
        public int Count => throw new InvalidOperationException("The count can not be retrieved for this generator");

        public Stream GetFileStream(PieFileEntry fileEntry)
        {
            if (fileEntry.ZipEntry == null)
                throw new InvalidOperationException("Can't read a file without a zip entry from the generator");

            MemoryStream memoryStream = new((int)fileEntry.ZipEntry.Length);
            using Stream zipStream = fileEntry.ZipEntry.Open();
            zipStream.CopyToEx(memoryStream);
            memoryStream.Position = 0;
            return memoryStream;
        }

        public void Dispose() { }
    }

    // Only really need this so we can correctly dispose all the archive instances
    private class PieArchiveData : IDisposable
    {
        public PieArchiveData(ZipArchive? mainZipArchive)
        {
            MainZipArchive = mainZipArchive;
        }

        public ZipArchive? MainZipArchive { get; }
        public List<ZipArchive> SubArchives { get; } = [];

        public void Dispose()
        {
            MainZipArchive?.Dispose();
            SubArchives.DisposeAll();
        }
    }

    private class PieFileEntry
    {
        public PieFileEntry(ZipArchiveEntry? zipEntry)
        {
            ZipEntry = zipEntry;
            LastWriteTime = zipEntry?.LastWriteTime ?? new DateTimeOffset(DateTime.Now);
            FileSize = zipEntry?.Length ?? 0;
        }

        public ZipArchiveEntry? ZipEntry { get; }
        public DateTimeOffset LastWriteTime { get; set; }
        public long FileSize { get; set; }
    }

    #endregion
}