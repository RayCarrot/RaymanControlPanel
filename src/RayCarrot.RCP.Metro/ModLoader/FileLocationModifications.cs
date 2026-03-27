using System.Diagnostics;
using System.IO;
using RayCarrot.RCP.Metro.Archive;
using RayCarrot.RCP.Metro.ModLoader.Resource;

namespace RayCarrot.RCP.Metro.ModLoader;

public class FileLocationModifications
{
    #region Constructor

    public FileLocationModifications(string location, IArchiveDataManager? archiveDataManager)
    {
        Location = location;
        ArchiveDataManager = archiveDataManager;
    }

    #endregion

    #region Logger

    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    #endregion

    #region Private Fields

    private readonly Dictionary<string, FileModification> _fileModifications = new();

    #endregion

    #region Public Properties

    public string Location { get; }
    public IArchiveDataManager? ArchiveDataManager { get; }
    public int FilesCount => _fileModifications.Count;

    #endregion

    #region Private Methods

    private void ModifyPhysicalFiles(
        LibraryFileHistoryBuilder historyBuilder,
        FileSystemPath dirPath,
        Action<Progress>? progressCallback)
    {
        int fileIndex = 0;

        // Process each file modification
        foreach (FileModification modification in _fileModifications.Values)
        {
            ModFilePath modFilePath = modification.ModFilePath;
            FileSystemPath physicalFilePath = dirPath + modFilePath.FilePath;

            // Make sure the file is inside of the directory. We want to disallow going up the tree using ..\
            // We also need to normalize the slashes since Path.GetFullPath will change that. It however doesn't change the casing.
            if (!Path.GetFullPath(physicalFilePath).Replace('/', '\\').StartsWith(dirPath.FullPath.Replace('/', '\\')))
            {
                Logger.Warn("File modification with path {0} is not valid", modFilePath.FilePath);
                continue;
            }

            using FileModificationStream fileStream = new(
                getStreamFunc: () => 
                {
                    Directory.CreateDirectory(physicalFilePath.Parent);
                    return File.Open(physicalFilePath, FileMode.OpenOrCreate, FileAccess.ReadWrite);
                },
                onStreamClosed: stream =>
                {
                    if (stream == null)
                    {
                        // Delete the file
                        physicalFilePath.DeleteFile();

                        // Delete the directory if it's empty
                        historyBuilder.DeleteDirectoryIfEmpty(physicalFilePath.Parent);
                    }
                });
            
            modification.ProcessFile(
                historyBuilder: historyBuilder,
                fileExists: physicalFilePath.FileExists,
                fileStream: fileStream);

            fileIndex++;
            progressCallback?.Invoke(new Progress(fileIndex, _fileModifications.Count));
        }
    }

    private ArchiveRepackResult? ModifyArchive(
        LibraryFileHistoryBuilder historyBuilder,
        FileSystemPath archiveFilePath,
        IArchiveDataManager manager,
        Action<Progress>? progressCallback)
    {
        if (!archiveFilePath.FileExists)
        {
            Logger.Warn("Archive {0} does not exist and its file modifications will be ignored", archiveFilePath);
            return null;
        }

        ArchiveRepackResult? repackResult = null;

        using TempFile archiveOutputFile = new(true);

        // Read the archive
        using (FileStream archiveStream = File.OpenRead(archiveFilePath))
        {
            string archiveFileName = archiveFilePath.Name;

            object? archive = null;
            ArchiveData? archiveData = null;

            // Files to be repacked
            List<FileItem> archiveFiles = new();

            try
            {
                archive = manager.LoadArchive(archiveStream, archiveFileName);
                archiveData = manager.LoadArchiveData(archive, archiveStream, archiveFileName);
                List<ArchiveDirectory> archiveDirectories = archiveData.Directories.ToList();

                Logger.Info("Modifying archive");

                Stopwatch sw = Stopwatch.StartNew();

                int initialModificationsCount = _fileModifications.Count;
                int totalFilesMax = archiveDirectories.Sum(x => x.Files.Length) + initialModificationsCount;
                totalFilesMax *= 2; // 0-50%
                int totalFilesIndex = 0;

                // Replace or remove existing files
                foreach (ArchiveDirectory dir in archiveDirectories)
                {
                    foreach (FileItem file in dir.Files)
                    {
                        progressCallback?.Invoke(new Progress(totalFilesIndex, totalFilesMax));
                        totalFilesIndex++;

                        string filePathKey = GetFilePathKey(manager.CombinePaths(file.Directory, file.FileName));

                        FileModification? modification = _fileModifications.TryGetValue(filePathKey);

                        if (modification == null)
                        {
                            // Keep the file and do nothing else
                            archiveFiles.Add(file);
                            continue;
                        }

                        _fileModifications.Remove(filePathKey);

                        using FileModificationStream fileStream = new(
                            // ReSharper disable once AccessToDisposedClosure
                            getStreamFunc: () => file.GetDecodedFileData(archiveData.Generator).Stream,
                            onStreamClosed: stream =>
                            {
                                if (stream != null)
                                {
                                    archiveFiles.Add(file);
                                    ReplaceArchiveFile(file, manager, stream);
                                }
                            });

                        modification.ProcessFile(
                            historyBuilder: historyBuilder,
                            fileExists: true,
                            fileStream: fileStream);
                    }
                }

                totalFilesIndex += initialModificationsCount - _fileModifications.Count;

                // Add files not already in the archive
                foreach (FileModification modification in _fileModifications.Values)
                {
                    progressCallback?.Invoke(new Progress(totalFilesIndex, totalFilesMax));
                    totalFilesIndex++;

                    using FileModificationStream fileStream = new(
                        // ReSharper disable once AccessToDisposedClosure
                        getStreamFunc: () => new MemoryStream(),
                        onStreamClosed: stream =>
                        {
                            if (stream != null)
                            {
                                // Get the file name and directory
                                string fileName = Path.GetFileName(modification.ModFilePath.FilePath);
                                string dir = Path.GetDirectoryName(modification.ModFilePath.FilePath)?.
                                    Replace(Path.DirectorySeparatorChar, manager.PathSeparatorCharacter) ?? String.Empty;

                                // Create a new archive entry
                                object entry = manager.GetNewFileEntry(archive, dir, fileName);

                                Logger.Trace("Adding file {0}", modification.ModFilePath);

                                // Create a new file item
                                FileItem file = new(manager, fileName, dir, entry);

                                // Add the file
                                archiveFiles.Add(file);

                                // Set the stream
                                ReplaceArchiveFile(file, manager, stream);
                            }
                        });

                    modification.ProcessFile(
                        historyBuilder: historyBuilder,
                        fileExists: false,
                        fileStream: fileStream);
                }

                sw.Stop();

                Logger.Info("Processed modified archive files in {0} ms", sw.ElapsedMilliseconds);

                // 50%
                progressCallback?.Invoke(new Progress(50, 100));

                sw.Restart();

                using ArchiveFileStream archiveOutputStream = new(File.OpenWrite(archiveOutputFile.TempPath),
                    archiveOutputFile.TempPath.Name, true);

                repackResult = manager.WriteArchive(
                    generator: archiveData.Generator,
                    archive: archive,
                    outputFileStream: archiveOutputStream,
                    files: archiveFiles,
                    loadState: new ProgressLoadState(
                        cancellationToken: CancellationToken.None,
                        progressCallback: x => progressCallback?.Invoke(new Progress(x.Percentage_100 * 0.5 + 50, 100)))); // 50-100%

                sw.Stop();

                Logger.Info("Repacked modified archive files in {0} ms", sw.ElapsedMilliseconds);
            }
            finally
            {
                if (archive is IDisposable disposable)
                    disposable.Dispose();

                archiveData?.Generator.Dispose();
                archiveFiles.DisposeAll();
            }
        }

        // Replace the archive with the modified one
        Services.File.MoveFile(archiveOutputFile.TempPath, archiveFilePath, true);

        return repackResult;
    }

    private void ReplaceArchiveFile(FileItem file, IArchiveDataManager manager, Stream resource)
    {
        Logger.Trace("Replacing archive file {0}/{1}", file.Directory, file.FileName);

        // Get the temp stream to store the pending import data
        file.SetPendingImport();

        resource.Position = 0;

        // Encode the data to the pending import stream
        manager.EncodeFile(resource, file.PendingImport, file.ArchiveEntry,
            // TODO: For now there is no point to supply metadata since the way the mod loader works, by using streams, means that
            //       metadata like file dates, attributes etc. get lost regardless of if it's a physical file or in an archive.
            new FileMetadata());

        // If no data was encoded we copy over the decoded data
        if (file.PendingImport.Length == 0)
            resource.CopyToEx(file.PendingImport);
    }

    private string GetFilePathKey(string filePath) => ModLoaderHelpers.NormalizePath(filePath);

    #endregion

    #region Public Methods

    public void AddFileModification(
        FileModification.FileType type,
        FileModification.FileSource source,
        ModFilePath modFilePath,
        FileModification.HistoryFileEntry? historyEntry = null,
        IModFileResource? resourceEntry = null,
        IFilePatch? filePatch = null)
    {
        string filePathKey = GetFilePathKey(modFilePath.FilePath);

        if (type == FileModification.FileType.PatchOnly)
        {
            // Make sure we have a patch
            if (filePatch == null)
                throw new Exception("Missing file patch entry");

            // Try get existing file modification
            if (!_fileModifications.TryGetValue(filePathKey, out FileModification? existingModification))
            {
                // If we could not get one then we create one
                existingModification = new FileModification(
                    type: type,
                    source: source,
                    modFilePath: modFilePath,
                    historyEntry: historyEntry,
                    resourceEntry: resourceEntry);
                _fileModifications[filePathKey] = existingModification;
            }

            existingModification.AddFilePatch(filePatch);
        }
        else
        {
            // If this modification overrides an existing one we want to copy over the history entry
            if (historyEntry == null &&
                _fileModifications.TryGetValue(filePathKey, out FileModification? existingModification))
            {
                historyEntry = existingModification.HistoryEntry;

                // If the existing modification has patches then we've added our modifications in the wrong order! Patches have to be added last.
                if (existingModification.HasFilePatches())
                    throw new Exception("Can not override a file modification with patches. The patches should always be added last!");
            }

            _fileModifications[filePathKey] = new FileModification(
                type: type,
                source: source,
                modFilePath: modFilePath,
                historyEntry: historyEntry,
                resourceEntry: resourceEntry);
        }
    }

    public ArchiveRepackResult? ApplyModifications(LibraryFileHistoryBuilder historyBuilder, FileSystemPath gameDir, Action<Progress>? progressCallback)
    {
        if (Location == String.Empty)
        {
            ModifyPhysicalFiles(
                historyBuilder: historyBuilder,
                dirPath: gameDir,
                progressCallback: progressCallback);

            return null;
        }
        else
        {
            IArchiveDataManager manager = ArchiveDataManager ?? throw new Exception($"No archive data manager for location {Location}");

            return ModifyArchive(
                historyBuilder: historyBuilder,
                archiveFilePath: gameDir + Location,
                manager: manager,
                progressCallback: progressCallback);
        }
    }

    #endregion
}