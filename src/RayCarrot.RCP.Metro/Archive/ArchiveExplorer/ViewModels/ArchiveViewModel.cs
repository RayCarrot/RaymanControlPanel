using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Windows.Input;
using Nito.AsyncEx;

namespace RayCarrot.RCP.Metro.Archive;

/// <summary>
/// View model for an archive from a file
/// </summary>
public class ArchiveViewModel : DirectoryViewModel
{
    #region Constructor

    /// <summary>
    /// Default constructor
    /// </summary>
    /// <param name="filePath">The file path for the archive</param>
    /// <param name="manager">The archive data manager</param>
    /// <param name="loaderViewModel">The view model to use when running an async operation which needs to load</param>
    /// <param name="explorerDialogViewModel">The explorer dialog view model</param>
    /// <param name="isDuplicateName">Indicates if the name of the archive matches the name of another loaded archive</param>
    public ArchiveViewModel(FileSystemPath filePath, IArchiveDataManager manager, LoaderViewModel loaderViewModel, ArchiveExplorerDialogViewModel explorerDialogViewModel, bool isDuplicateName) : base(null, filePath.Name, null)
    {
        Logger.Info("An archive view model is being created for {0}", filePath.Name);

        // Set properties
        FilePath = filePath;
        Manager = manager;
        LoaderViewModel = loaderViewModel;
        ExplorerDialogViewModel = explorerDialogViewModel;
        IsDuplicateName = isDuplicateName;
        ThumbnailCache = new ThumbnailCache();

        // Create commands
        SaveCommand = new AsyncRelayCommand(SaveAsync);
        OpenLocationCommand = new AsyncRelayCommand(OpenLocationAsync);

        // Open the file stream
        OpenFile();
    }

    #endregion

    #region Logger

    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    #endregion

    #region Protected Properties

    /// <summary>
    /// The current amount of modified files
    /// </summary>
    protected int ModifiedFilesCount { get; set; }

    #endregion

    #region Commands

    public ICommand SaveCommand { get; }

    public ICommand OpenLocationCommand { get; }

    #endregion

    #region Public Properties

    /// <summary>
    /// Indicates if the name of the archive matches the name of another loaded archive
    /// </summary>
    public bool IsDuplicateName { get; }

    /// <summary>
    /// The explorer dialog view model
    /// </summary>
    public ArchiveExplorerDialogViewModel ExplorerDialogViewModel { get; }

    /// <summary>
    /// The archive file generator
    /// </summary>
    public IDisposable? ArchiveFileGenerator { get; set; }

    /// <summary>
    /// The view model to use when running an async operation which needs to load
    /// </summary>
    public LoaderViewModel LoaderViewModel { get; }

    /// <summary>
    /// The archive file stream
    /// </summary>
    public FileStream ArchiveFileStream { get; protected set; }

    /// <summary>
    /// The data for the loaded archive
    /// </summary>
    public object? ArchiveData { get; set; }

    /// <summary>
    /// The lock to use when accessing the archive stream
    /// </summary>
    public AsyncLock ArchiveLock => ExplorerDialogViewModel.ArchiveLock;

    /// <summary>
    /// The file path for the archive
    /// </summary>
    public FileSystemPath FilePath { get; }

    /// <summary>
    /// The description to display
    /// </summary>
    public override string DisplayDescription => FilePath.FullPath;

    /// <summary>
    /// The name of the item to display
    /// </summary>
    public override string DisplayName => IsDuplicateName ? $"{(FileSystemPath)FilePath.Parent.Name + FilePath.Name}" : FilePath.Name;

    /// <summary>
    /// The name of the item
    /// </summary>
    public string Name => FilePath.Name;

    /// <summary>
    /// The name of the directory to use when exporting
    /// </summary>
    public override string ExportDirName => FilePath.RemoveFileExtension().Name;

    /// <summary>
    /// The archive data manager
    /// </summary>
    public IArchiveDataManager Manager { get; }

    /// <summary>
    /// Gets the currently selected item
    /// </summary>
    public DirectoryViewModel? SelectedItem => this.GetAllChildren<DirectoryViewModel>(true).FirstOrDefault(x => x.IsSelected);
       
    /// <summary>
    /// The text to display for the save prompt if there are modified files
    /// </summary>
    public string? ModifiedFilesDisplayText { get; protected set; }

    /// <summary>
    /// Indicates if there are modified files
    /// </summary>
    public bool HasModifiedFiles { get; protected set; }

    public ThumbnailCache ThumbnailCache { get; }

    #endregion

    #region Protected Methods

    [MemberNotNull(nameof(ArchiveFileStream))]
    protected void OpenFile()
    {
        ArchiveFileStream = new FileStream(FilePath, FileMode.Open, FileAccess.ReadWrite, FileShare.Read);
    }

    /// <summary>
    /// Clears and disposes every item
    /// </summary>
    protected void ClearAndDisposeItems()
    {
        Logger.Info("The archive items have been cleared and disposed");

        // Dispose every directory
        this.GetAllChildren<DirectoryViewModel>().DisposeAll();

        // Dispose files
        Files.DisposeAll();

        // Clear the items
        Clear();
        Files.Clear();
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// Loads the archive
    /// </summary>
    public void LoadArchive()
    {
        Logger.Info("The archive {0} is being loaded", DisplayName);

        // Clear existing items
        ClearAndDisposeItems();

        // Indicate that no files have pending edits
        ModifiedFilesCount = 0;
        HasModifiedFiles = false;

        // Dispose the current archive data
        if (ArchiveData is IDisposable disposable)
            disposable.Dispose();

        // Load the archive data
        ArchiveData = Manager.LoadArchive(ArchiveFileStream, Name);

        // Load the archive
        ArchiveData data = Manager.LoadArchiveData(ArchiveData, ArchiveFileStream, FilePath.Name);

        // Dispose the current generator
        ArchiveFileGenerator?.Dispose();

        // Save the generator
        ArchiveFileGenerator = data.Generator;

        // Add each directory
        foreach (ArchiveDirectory dir in data.Directories)
        {
            // Check if it's the root directory
            if (dir.DirectoryName == String.Empty)
            {
                // Add the files
                Files.AddRange(dir.Files.Select(x => new FileViewModel(x, this)));

                continue;
            }

            // Keep track of the previous item
            DirectoryViewModel prevItem = this;

            // Enumerate each sub directory
            foreach (string subDir in dir.DirectoryName.Trim(Manager.PathSeparatorCharacter).Split(Manager.PathSeparatorCharacter))
            {
                // Set the previous item and create the item if it doesn't already exist
                prevItem = prevItem.FirstOrDefault(x => x.ID == subDir) ?? prevItem.Add(subDir);
            }

            // Add the files
            prevItem.Files.AddRange(dir.Files.Select(x => new FileViewModel(x, prevItem)));
        }
    }

    /// <summary>
    /// Saves any pending changes to the archive and reloads it
    /// </summary>
    /// <returns>The task</returns>
    public async Task SaveAsync()
    {
        Logger.Info("The archive {0} is being repacked", DisplayName);

        // Make sure we can repack the archive by checking if the file is locked
        Process[]? lockProcesses = null;
        Process? currentProcess = null;
        try
        {
            lockProcesses = WindowsHelpers.GetProcessesLockingFile(FilePath);
            currentProcess = Process.GetCurrentProcess();
            int currentId = currentProcess.Id;

            if (lockProcesses.Any(x => x.Id != currentId))
            {
                await Services.MessageUI.DisplayMessageAsync(String.Format(Resources.Archive_RepackLocked,
                    lockProcesses.
                        Where(x => x.Id != currentId).
                        Select(x => $"- {x.ProcessName} ({x.MainModule?.ModuleName})").
                        JoinItems(Environment.NewLine)), MessageType.Error);
                return;
            }
        }
        finally
        {
            lockProcesses?.DisposeAll();
            currentProcess?.Dispose();
        }

        // Run as a load operation
        using (LoaderLoadState state = await Archive.LoaderViewModel.RunAsync(String.Format(Resources.Archive_RepackingStatus, DisplayName), canCancel: true))
        {
            // Lock the access to the archive
            using (await Archive.ArchiveLock.LockAsync())
            {
                // Find the selected item path
                string? selectedDirAddr = ExplorerDialogViewModel.SelectedDir == null
                    ? null
                    : ExplorerDialogViewModel.GetDirectoryAddress(ExplorerDialogViewModel.SelectedDir);

                // Run as a task
                await Task.Run(async () =>
                {
                    // Stop file initialization
                    if (ExplorerDialogViewModel.IsInitializingFiles)
                        ExplorerDialogViewModel.CancelInitializeFiles = true;

                    try
                    {
                        // Get a temporary file path to write to
                        using TempFile tempOutputFile = new(false);

                        const double repackProgress = 1.0;
                        double onRepackedProgress = Manager.GetOnRepackedArchivesProgressLength();

                        Progress currentProgress = new(0, repackProgress + onRepackedProgress);

                        // Create the file and get the stream
                        ArchiveRepackResult repackResult;
                        using (ArchiveFileStream outputStream = new(File.Create(tempOutputFile.TempPath),
                                   tempOutputFile.TempPath.Name, true))
                        {
                            // Write to the stream
                            repackResult = Manager.WriteArchive(
                                generator: ArchiveFileGenerator,
                                archive: ArchiveData ?? throw new Exception("Archive data has not been loaded"),
                                outputFileStream: outputStream,
                                files: this.GetAllChildren<DirectoryViewModel>(true).SelectMany(x => x.Files)
                                    .Select(x => x.FileData),
                                // ReSharper disable once AccessToDisposedClosure
                                loadState: new PartialProgressLoadState(state, x => currentProgress.Add(x, repackProgress)));
                        }

                        currentProgress += repackProgress;

                        state.SetStatus(Resources.Archive_SavingStatus);
                        state.SetCanCancel(false);

                        // Dispose the archive file stream
                        ArchiveFileStream.Dispose();

                        if (ArchiveData is IDisposable disposable)
                            disposable.Dispose();
                        ArchiveData = null;

                        // If the operation succeeded, replace the archive file with the temporary output
                        Services.File.MoveFile(tempOutputFile.TempPath, FilePath, true);

                        // On repack
                        await Manager.OnRepackedArchivesAsync(
                            archiveFilePaths: new[] { FilePath },
                            repackResults: new[] { repackResult },
                            loadState: new PartialProgressLoadState(state, x => currentProgress.Add(x, onRepackedProgress)));

                        currentProgress += onRepackedProgress;

                        // Re-open the file stream
                        OpenFile();

                        state.Complete();
                    }
                    catch (OperationCanceledException ex)
                    {
                        Logger.Trace(ex, "Cancelled repacking archive");
                    }
                    catch (Exception ex)
                    {
                        Logger.Error(ex, "Repacking archive {0}", DisplayName);

                        state.Error();

                        await Services.MessageUI.DisplayExceptionMessageAsync(ex, Resources.Archive_RepackError);

                        // Re-open the file stream if closed
                        if (ArchiveFileStream.SafeFileHandle?.IsClosed != false)
                            OpenFile();
                    }
                });

                // TODO: Ideally we won't reload the archive if the repacking was stopped due to an error or
                //       canceled by the user. The problem preventing this from working right now is that
                //       WriteArchive() will modify the archive object and the file entries. One solution
                //       would be to clone them instead of modifying the existing ones.
                // Reload the archive
                LoadArchive();

                // Load the previously selected directory if it still exists
                if (selectedDirAddr != null)
                    ExplorerDialogViewModel.LoadDirectory(selectedDirAddr);
            }
        }
    }

    /// <summary>
    /// Opens the location of the archive
    /// </summary>
    /// <returns>The task</returns>
    public async Task OpenLocationAsync()
    {
        // Open the location
        await Services.File.OpenExplorerLocationAsync(FilePath);

        Logger.Trace("The archive {0} location was opened", DisplayName);
    }

    /// <summary>
    /// Adds the specified number of new files as being modified
    /// </summary>
    /// <param name="count">The number of new modified files to add</param>
    /// <param name="allow0">Indicates if a count of 0 should be allowed</param>
    public void AddModifiedFiles(int count = 1, bool allow0 = false)
    {
        if (count == 0 && !allow0)
            return;

        // Increment by the count
        ModifiedFilesCount += count;

        // Update text
        ModifiedFilesDisplayText = $"{ModifiedFilesCount} files have been modified in {DisplayName}";

        // Update boolean
        HasModifiedFiles = true;
    }

    /// <summary>
    /// Disposes the archive and its folders and files
    /// </summary>
    public override void Dispose()
    {
        // Cancel refreshing thumbnails
        ExplorerDialogViewModel.CancelInitializeFiles = true;

        // Dispose base class
        base.Dispose();

        // Dispose the stream
        ArchiveFileStream.Dispose();

        // Dispose every directory
        ClearAndDisposeItems();

        // Dispose the archive data
        if (ArchiveData is IDisposable disposable)
            disposable.Dispose();

        // Dispose the generator
        ArchiveFileGenerator?.Dispose();

        // Dispose the cache
        ThumbnailCache.Dispose();

        Logger.Info("The archive {0} has been disposed", DisplayName);
    }

    #endregion
}