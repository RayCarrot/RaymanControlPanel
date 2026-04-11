using System.Diagnostics;
using System.IO;
using System.Security.Cryptography;

namespace RayCarrot.RCP.Metro;

public class FileEditing : IDisposable
{
    public FileEditing(string fileName)
    {
        _tempDir = new TempDirectory(true);
        FilePath = _tempDir.TempPath + fileName;
    }

    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    private readonly TempDirectory _tempDir;

    public FileSystemPath FilePath { get; }

    private byte[] ComputeFileHash(HashAlgorithm hashAlgorithm)
    {
        using FileStream tmpFile = File.OpenRead(FilePath);
        return hashAlgorithm.ComputeHash(tmpFile);
    }

    public async Task<bool> ExecuteAsync(string fileExtension, bool readOnly, LoaderLoadState state, Action<FileSystemPath> createFileAction)
    {
        // Get the program to open the file with
        FileSystemPath? programPath = await Services.AssociatedFileEditorsManager.RequestFileEditorAssociatonAsync(fileExtension);

        if (programPath == null)
            return false;

        // Export the file
        createFileAction(FilePath);

        using HashAlgorithm sha1 = HashAlgorithm.Create();

        // Get the original file hash
        byte[] originalHash = ComputeFileHash(sha1);

        // If read-only set the attribute
        if (readOnly)
        {
            FileInfo info = FilePath.GetFileInfo();
            info.Attributes |= FileAttributes.ReadOnly;
        }

        bool canceled = false;

        Process? process = null;
        try
        {
            // Open the process
            process = await Services.File.LaunchFileAsync(programPath.Value, arguments: $"\"{FilePath}\"");

            // Ignore if the file wasn't opened
            if (process == null)
            {
                Logger.Trace("The file was not opened");
                return false;
            }

            state.SetStatus(String.Format(Resources.WaitForEditorToClose, programPath.Value.RemoveFileExtension().Name));
            state.SetCanCancel(true);

            try
            {
                Stopwatch sw = Stopwatch.StartNew();

                // Wait for the process to close...
                await process.WaitForExitAsync(state.CancellationToken);

                sw.Stop();

                // If the process closed within 2 seconds we can assume that it opened the file
                // in an already opened process, so we try and find that instead
                if (sw.Elapsed < TimeSpan.FromSeconds(2))
                {
                    process.Dispose();
                    Process[] otherProcesses = Process.GetProcessesByName(programPath.Value.RemoveFileExtension().Name);

                    // If exactly one was found then we wait for that now instead
                    if (otherProcesses.Length == 1)
                    {
                        process = otherProcesses[0];
                        await process.WaitForExitAsync(state.CancellationToken);
                    }
                }
            }
            catch (OperationCanceledException ex)
            {
                Logger.Trace(ex, "Canceled editing file");

                canceled = true;

                try
                {
                    // Attempt to close the process
                    process.CloseMainWindow();

                    // Wait 2 seconds just to give it time to fully close. We could await it
                    // closing, but we don't want to soft-lock here if the process doesn't close
                    await Task.Delay(TimeSpan.FromSeconds(2));
                }
                catch (Exception ex2)
                {
                    Logger.Warn(ex2, "Closing file editor process after cancellation");
                }
            }

            state.SetStatus(String.Empty);
            state.SetCanCancel(false);
        }
        finally
        {
            process?.Dispose();
        }

        // Remove the read-only attribute
        if (readOnly)
        {
            FileInfo info = FilePath.GetFileInfo();
            info.Attributes &= ~FileAttributes.ReadOnly;
        }

        // If canceled or read-only we don't need to check if it has been modified
        if (canceled || readOnly)
            return false;

        // Get the new hash
        byte[] newHash = ComputeFileHash(sha1);

        // Check if the file was modified
        bool modified = !originalHash.SequenceEqual(newHash);

        // Check if the file has been modified
        if (!originalHash.SequenceEqual(newHash))
            Logger.Trace("The file was modified");
        else
            Logger.Trace("The file was not modified");

        return modified;
    }

    public void Dispose()
    {
        _tempDir.Dispose();
    }
}