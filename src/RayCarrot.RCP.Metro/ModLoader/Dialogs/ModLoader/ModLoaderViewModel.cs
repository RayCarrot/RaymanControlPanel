using System.IO;
using System.Net.Http;
using System.Windows.Input;
using Newtonsoft.Json.Linq;
using RayCarrot.RCP.Metro.Games.Components;
using RayCarrot.RCP.Metro.Games.Structure;
using RayCarrot.RCP.Metro.ModLoader.Extractors;
using RayCarrot.RCP.Metro.ModLoader.Library;
using RayCarrot.RCP.Metro.ModLoader.Metadata;
using RayCarrot.RCP.Metro.ModLoader.Resource;
using RayCarrot.RCP.Metro.ModLoader.Sources;

namespace RayCarrot.RCP.Metro.ModLoader.Dialogs.ModLoader;

public class ModLoaderViewModel : BaseViewModel, IDisposable
{
    #region Constructor

    public ModLoaderViewModel(GameInstallation gameInstallation, ModToInstall[]? pendingModFiles = null, Func<ModLoaderViewModel, Task>? customInitAction = null)
    {
        GameInstallation = gameInstallation;
        _pendingModFiles = pendingModFiles;
        _customInitAction = customInitAction;

        // Verify the game supports mods
        if (!gameInstallation.GetComponents<ModModuleComponent>().Any())
            throw new InvalidOperationException("The game installation doesn't support mods");

        _httpClient = Services.HttpClientFactory.CreateClient();
        _modExtractors = ModExtractor.GetModExtractors();

        LoaderViewModel = new LoaderViewModel();
        Mods = new ObservableCollection<ModViewModel>();

        ModProcessor = new ModProcessor(GameInstallation);
        Library = new ModLibrary(GameInstallation);

        ModifiedFiles = new ModifiedFilesViewModel(GameInstallation);
        DownloadableMods = new DownloadableModsViewModel(this, GameInstallation, _httpClient, DownloadableModsSource.GetSources().ToList());

        InstallModFromFileCommand = new AsyncRelayCommand(InstallModFromFileAsync) { AllowConcurrentExecutions = true };
        CheckForUpdatesCommand = new AsyncRelayCommand(CheckForUpdatesAsync);
    }

    #endregion

    #region Logger

    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    #endregion

    #region Private Fields

    private ModToInstall[]? _pendingModFiles;
    private Func<ModLoaderViewModel, Task>? _customInitAction;
    private readonly HttpClient _httpClient;
    private readonly ModExtractor[] _modExtractors;

    #endregion

    #region Commands

    public ICommand InstallModFromFileCommand { get; }
    public ICommand CheckForUpdatesCommand { get; }

    #endregion

    #region Public Properties

    public GameInstallation GameInstallation { get; }
    public ModProcessor ModProcessor { get; }
    public ModLibrary Library { get; }
    public LoaderViewModel LoaderViewModel { get; }
    
    public ModManifest? ModManifest { get; set; }
    public ObservableCollection<ModViewModel> Mods { get; }
    public ModViewModel? SelectedMod { get; set; }

    public ModifiedFilesViewModel ModifiedFiles { get; }
    public bool ShowModifiedFilesAsTree
    {
        get => ModifiedFiles.ShowModifiedFilesAsTree;
        set
        {
            ModifiedFiles.ShowModifiedFilesAsTree = value;
            RefreshModifiedFiles();
        }
    }

    public LocalizedString? AddedFilesText { get; set; }
    public LocalizedString? RemovedFilesText { get; set; }
    public LocalizedString? PatchedFilesText { get; set; }
    public LocalizedString? ChangedModsText { get; set; }

    public bool HasChanges { get; set; }
    public bool HasReorderedMods { get; set; }

    public DownloadableModsViewModel DownloadableMods { get; }

    #endregion

    #region Private Methods

    private async Task MigrateLegacyPatchesAsync()
    {
        LegacyPatchMigrationManager manager = new(GameInstallation, Library);

        if (!manager.CanMigrate())
            return;

        using (LoaderLoadState state = await LoaderViewModel.RunAsync(Resources.ModLoader_MigratingPatchesStatus))
        {
            try
            {
                await Task.Run(async () => await manager.MigrateAsync(state.SetProgress));
                state.Complete();
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Migrating legacy patches");

                state.Error();

                await Services.MessageUI.DisplayExceptionMessageAsync(ex, Resources.ModLoader_MigratingPatchesError);
            }
        }
    }

    private async Task<bool> LoadInstalledModsAsync()
    {
        Logger.Info("Loading installed mods");

        try
        {
            // Clear any previously loaded mods
            lock (Mods)
            {
                Mods.DisposeAll();
                Mods.Clear();
            }

            ModManifest = Library.ReadModManifest();

            foreach (ModManifestEntry modEntry in ModManifest.Mods.Values)
            {
                Mod mod;

                try
                {
                    // Read the mod
                    mod = Library.ReadInstalledMod(modEntry.Id, GameInstallation);
                }
                catch (UnsupportedModVersionException ex)
                {
                    Logger.Warn(ex, "Reading installed mod");

                    await Services.MessageUI.DisplayMessageAsync(Resources.ModLoader_ReadingModVersionError, MessageType.Error);
                    return false;
                }
                catch (Exception ex)
                {
                    Logger.Error(ex, "Reading installed mod");

                    await Services.MessageUI.DisplayExceptionMessageAsync(ex, Resources.ModLoader_ReadingModError);
                    return false;
                }

                // Create and add view model
                ModViewModel vm = new(
                    modLoaderViewModel: this, 
                    loaderViewModel: LoaderViewModel, 
                    downloadableModsSource: DownloadableModsSource.GetSource(modEntry.InstallInfo));
                vm.InitDownloaded(ModViewModel.ModInstallState.Installed, mod, modEntry);
                
                lock (Mods)
                    Mods.Add(vm);

                Logger.Info("Added installed mod '{0}' from library with version {1} and ID {2}", 
                    vm.DownloadedMod.Metadata.Name, vm.DownloadedMod.Metadata.Version, vm.DownloadedMod.Metadata.Id);
            }

            Logger.Info("Loaded {0} installed mods", ModManifest.Mods.Count);

            RefreshModifiedFiles();
            RefreshDependencies();

            return true;
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Loading installed mods");

            await Services.MessageUI.DisplayExceptionMessageAsync(ex, Resources.ModLoader_ReadingModsError);

            return false;
        }
    }

    private async Task<bool> VerifyModSecurityAsync(Mod mod)
    {
        // Check if the patch adds or replaces exe or dll files. Expand to check other file types too?
        bool hasCodeFiles = mod.GetAddedFiles().
            Select(x => x.Path.FilePath).
            Concat(mod.GetPatchedFiles().Select(x => x.Path.FilePath)).
            Any(x => x.EndsWith(".exe", StringComparison.InvariantCultureIgnoreCase) ||
                     x.EndsWith(".dll", StringComparison.InvariantCultureIgnoreCase));

        // Have the user verify
        if (hasCodeFiles)
        {
            Logger.Info("Mod with id {0} contains one or more potentially harmful files", mod.Metadata.Id);

            return await Services.MessageUI.DisplayMessageAsync(String.Format(Resources.ModLoader_SecurityWarning, mod.Metadata.Name), MessageType.Question, true);
        }
        else
        {
            return true;
        }
    }

    private async Task<ModMetadata?> AddModToInstallAsync(
        FileSystemPath filePath, 
        Action<Progress> progressCallback, 
        CancellationToken cancellationToken, 
        string? sourceId, 
        object? installData,
        ModViewModel tempDownloadMod,
        ModViewModel? existingMod = null)
    {
        FileExtension fileExtension = filePath.FileExtension;
        ModExtractor modExtractor = _modExtractors.First(x => x.FileExtension == fileExtension);

        TempDirectory extractTempDir = new(true);

        try
        {
            // Extract the mod
            await Task.Run(async () =>
                await modExtractor.ExtractAsync(filePath, extractTempDir.TempPath, progressCallback, cancellationToken));

            // Read the mod
            Mod extractedMod = new(extractTempDir.TempPath, GameInstallation);

            // Verify game
            if (!extractedMod.Metadata.IsGameValid(GameInstallation.GameDescriptor))
            {
                Logger.Warn("Failed to add mod due to the current game {0} not being supported", GameInstallation.FullId);

                IEnumerable<LocalizedString> gameTargets = extractedMod.Metadata.Games.Select(x => Services.Games.GetGameDescriptor(x).DisplayName);

                await Services.MessageUI.DisplayMessageAsync(String.Format(Resources.ModLoader_InstallInvalidTargetError,
                    extractedMod.Metadata.Name, String.Join(Environment.NewLine, gameTargets)), MessageType.Error);

                extractTempDir.Dispose();

                return null;
            }

            // Verify the security
            if (!await VerifyModSecurityAsync(extractedMod))
            {
                extractTempDir.Dispose();
                return null;
            }

            string id = extractedMod.Metadata.Id;
            long size = extractTempDir.TempPath.GetSize();
            ModInstallInfo installInfo = new(
                Source: sourceId, 
                Version: extractedMod.Metadata.Version, 
                Size: size, 
                Date: DateTime.Now, 
                Data: installData == null ? null : JObject.FromObject(installData));

            List<ModDependencyInfo> modDependencies = new();
            lock (Mods)
            {
                // Attempt to find existing mod if not specified
                existingMod ??= Mods.FirstOrDefault(x => x.IsDownloaded && x.DownloadedMod.Metadata.Id == id);

                // Create a new manifest entry for the mod
                ModManifestEntry modEntry = new(id, installInfo, existingMod?.IsEnabled ?? true);

                // Create the mod view model
                ModViewModel mod = new(
                    modLoaderViewModel: this,
                    loaderViewModel: LoaderViewModel,
                    downloadableModsSource: DownloadableModsSource.GetSource(modEntry.InstallInfo))
                {
                    PendingInstallTempDir = extractTempDir
                };
                mod.InitDownloaded(ModViewModel.ModInstallState.PendingInstall, extractedMod, modEntry);

                // The mod is replacing an existing mod
                if (existingMod != null && 
                    existingMod.IsDownloaded && 
                    existingMod.DownloadedMod.Metadata.Id == id && 
                    Mods.IndexOf(existingMod) is var existingModIndex && 
                    existingModIndex != -1)
                {
                    // Dispose and replace the existing mod
                    existingMod.Dispose();
                    Mods[existingModIndex] = mod;

                    // Dispose and remove the temp download mod
                    tempDownloadMod.Dispose();
                    Mods.Remove(tempDownloadMod);
                }
                // The mod is being added as a new mod
                else
                {
                    // Check if the mod has a dependant which requires a custom sort
                    ModDependencyInfo[]? dependantModDependencies = null;
                    int dependantModIndex = -1;
                    int sort = 0;
                    for (int addedModIndex = 0; addedModIndex < Mods.Count; addedModIndex++)
                    {
                        ModViewModel addedMod = Mods[addedModIndex];
                        if (addedMod.IsDownloaded &&
                            addedMod.InstallState != ModViewModel.ModInstallState.PendingUninstall &&
                            addedMod.DownloadedMod.Metadata.Dependencies != null)
                        {
                            foreach (ModDependencyInfo dependency in addedMod.DownloadedMod.Metadata.Dependencies)
                            {
                                if (dependency.Ids.Contains(id) && dependency.Sort != 0)
                                {
                                    dependantModDependencies = addedMod.DownloadedMod.Metadata.Dependencies;
                                    dependantModIndex = addedModIndex;
                                    sort = -dependency.Sort; // NOTE: Sort is reversed!
                                    break;
                                }
                            }

                            if (dependantModDependencies != null)
                                break;
                        }
                    }

                    // No dependant mod with custom sort
                    if (dependantModDependencies == null)
                    {
                        // Dispose and replace the temp download mod
                        tempDownloadMod.Dispose();
                        Mods[Mods.IndexOf(tempDownloadMod)] = mod;
                    }
                    else
                    {
                        // Find where to insert the mod based on the dependency sort values
                        int insertIndex;
                        if (sort > 0)
                        {
                            insertIndex = dependantModIndex + 1;
                            for (int addedModIndex = dependantModIndex + 1; addedModIndex < Mods.Count; addedModIndex++)
                            {
                                ModViewModel addedMod = Mods[addedModIndex];
                                if (addedMod.IsDownloaded)
                                {
                                    string addedModId = addedMod.DownloadedMod.Metadata.Id;
                                    ModDependencyInfo? dependency = dependantModDependencies.FirstOrDefault(x => x.Ids.Contains(addedModId));
                                    if (dependency != null && dependency.Sort != 0)
                                    {
                                        if (sort > -dependency.Sort)
                                        {
                                            insertIndex = addedModIndex + 1;
                                        }
                                        else if (sort < -dependency.Sort)
                                        {
                                            break;
                                        }
                                    }
                                }
                            }
                        }
                        else
                        {
                            insertIndex = dependantModIndex;
                            for (int addedModIndex = dependantModIndex - 1; addedModIndex >= 0; addedModIndex--)
                            {
                                ModViewModel addedMod = Mods[addedModIndex];
                                if (addedMod.IsDownloaded)
                                {
                                    string addedModId = addedMod.DownloadedMod.Metadata.Id;
                                    ModDependencyInfo? dependency = dependantModDependencies.FirstOrDefault(x => x.Ids.Contains(addedModId));
                                    if (dependency != null && dependency.Sort != 0)
                                    {
                                        if (sort < -dependency.Sort)
                                        {
                                            insertIndex = addedModIndex;
                                        }
                                        else if (sort > -dependency.Sort)
                                        {
                                            break;
                                        }
                                    }
                                }
                            }
                        }

                        // Insert the mod
                        Mods.Insert(insertIndex, mod);

                        // Remove and dispose the temp download mod
                        tempDownloadMod.Dispose();
                        Mods.Remove(tempDownloadMod);
                    }
                }

                // Remove any mods with the same id
                while (Mods.FirstOrDefault(x => x != mod && x.IsDownloaded && x.DownloadedMod.Metadata.Id == id) is { } conflictMod)
                {
                    Logger.Info("Removing mod due to mod ID conflict");
                    conflictMod.Dispose();
                    Mods.Remove(conflictMod);
                    Services.Messenger.Send(new RemovedModFromLibraryMessage(GameInstallation, conflictMod.DownloadableModsSource?.Id, conflictMod.InstallData));
                }

                Logger.Info("Added new mod to install with ID {0}", extractedMod.Metadata.Id);

                // Get the dependencies which are not currently installed
                if (extractedMod.Metadata.Dependencies != null)
                {
                    foreach (ModDependencyInfo dependencyInfo in extractedMod.Metadata.Dependencies)
                    {
                        if (Mods.Any(x => x.InstallState != ModViewModel.ModInstallState.PendingUninstall &&
                                          x.IsDownloaded && 
                                          dependencyInfo.Ids.Contains(x.DownloadedMod.Metadata.Id)))
                            continue;

                        modDependencies.Add(dependencyInfo);
                    }
                }
            }

            // Handle mod dependencies
            if (modDependencies.Any())
            {
                Logger.Info("Detected {0} dependencies for mod with ID {1}", modDependencies.Count, extractedMod.Metadata.Id);

                // Ask user to download the dependencies
                if (await Services.MessageUI.DisplayMessageAsync(
                        String.Format(
                            Resources.ModLoader_DownloadDependencies,
                            extractedMod.Metadata.Name,
                            modDependencies.Select(x => $"- {x.Name}").JoinItems(Environment.NewLine)),
                        Resources.ModLoader_DownloadDependenciesHeader, MessageType.Question, true))
                {
                    List<Task> downloadTasks = new();

                    // Download the mods, grouped for each source
                    foreach (var deps in modDependencies.GroupBy(x => x.SourceId))
                    {
                        DownloadableModsSource? source = DownloadableModsSource.GetSource(deps.Key);
                       
                        if (source != null)
                        {
                            ModDownload[] downloads;
                            try
                            {
                                downloads = await source.DownloadModDependenciesAsync(_httpClient, GameInstallation, deps);
                            }
                            catch (Exception ex)
                            {
                                Logger.Error(ex, "Downloading mod dependencies for source {0}", deps.Key);
                                await Services.MessageUI.DisplayExceptionMessageAsync(ex, Resources.ModLoader_DownloadDependenciesError);
                                break;
                            }

                            // Download the mods
                            foreach (ModDownload download in downloads)
                            {
                                downloadTasks.Add(InstallModFromDownloadableFileAsync(
                                    source: source,
                                    existingMod: null,
                                    fileName: download.FileName,
                                    downloadUrl: download.DownloadUrl,
                                    fileSize: download.FileSize,
                                    installData: download.InstallData,
                                    modName: download.ModName));
                            }
                        }
                        else
                        {
                            Logger.Warn("Defined mod dependency source {0} is invalid", deps.Key);
                        }
                    }

                    // Await the downloads
                    await Task.WhenAll(downloadTasks);
                }
            }

            return extractedMod.Metadata;
        }
        catch
        {
            extractTempDir.Dispose();
            throw;
        }
    }

    private long GetLargestArchiveSize(IEnumerable<ModViewModel> mods)
    {
        HashSet<string> checkedArchives = new();
        long largestSize = 0;

        // Check history first since these will be reverted
        LibraryFileHistory? history = Library.ReadHistory();
        if (history != null)
        {
            foreach (ModFilePath addedFile in history.AddedFiles)
                checkArchiveSize(addedFile);

            foreach (ModFilePath removedFile in history.RemovedFiles)
                checkArchiveSize(removedFile);

            foreach (ModFilePath replacedFile in history.ReplacedFiles)
                checkArchiveSize(replacedFile);
        }

        // Then check enabled mods
        foreach (ModViewModel modViewModel in mods)
        {
            if (!modViewModel.IsDownloaded || !modViewModel.IsEnabled) 
                continue;
            
            Mod mod = modViewModel.DownloadedMod.Mod;

            foreach (IModFileResource addedFile in mod.GetAddedFiles()) 
                checkArchiveSize(addedFile.Path);

            foreach (ModFilePath removedFile in mod.GetRemovedFiles()) 
                checkArchiveSize(removedFile);

            foreach (IFilePatch patchedFile in mod.GetPatchedFiles()) 
                checkArchiveSize(patchedFile.Path);
        }

        return largestSize;

        void checkArchiveSize(ModFilePath filePath)
        {
            if (!filePath.HasLocation) 
                return;
            
            if (!checkedArchives.Add(filePath.Location))
                return;

            FileSystemPath archiveFilePath = GameInstallation.InstallLocation.Directory + filePath.Location;

            if (archiveFilePath.FileExists)
            {
                long fileSize = archiveFilePath.GetSize();

                if (fileSize > largestSize)
                    largestSize = fileSize;
            }
        }
    }

    private bool IsFilePatched(string filePath, IEnumerable<ModViewModel> mods)
    {
        foreach (ModViewModel modViewModel in mods)
        {
            if (modViewModel.IsDownloaded &&
                modViewModel.IsEnabled &&
                modViewModel.InstallState != ModViewModel.ModInstallState.PendingUninstall)
            {
                foreach (IFilePatch filePatch in modViewModel.DownloadedMod.Mod.GetPatchedFiles())
                {
                    if (!filePatch.Path.HasLocation && filePatch.Path.FilePath.Equals(filePath, StringComparison.InvariantCultureIgnoreCase))
                    {
                        return true;
                    }
                }
            }
        }

        return false;
    }

    #endregion

    #region Public Static Methods

    public static async Task<ModLoaderViewModel?> FromFilesAsync(GameInstallation? gameInstallation, ModToInstall[] modFilePaths)
    {
        if (modFilePaths.Length == 0)
            throw new ArgumentException("There has to be a least one mod file provided", nameof(modFilePaths));

        if (gameInstallation == null)
        {
            // Use the first file to determine which games are being targeted
            ModToInstall firstFile = modFilePaths[0];
            FileExtension firstFileExtension = firstFile.FilePath.FileExtension;
            ModExtractor? modExtractor = ModExtractor.GetModExtractors().FirstOrDefault(x => x.FileExtension == firstFileExtension);

            if (modExtractor == null)
                throw new Exception("One or more mod files are not valid");

            string[] gameTargets = modExtractor.GetGameTargets(firstFile.FilePath);

            // Get all the installations which the patch supports
            List<GameInstallation> gameInstallations = Services.Games.GetInstalledGames().
                Where(x => gameTargets.Contains(x.GameDescriptor.GameId)).
                ToList();

            // Make sure there is an installed game which can be patched
            if (!gameInstallations.Any())
            {
                string gameTargetNames = String.Join(Environment.NewLine, gameTargets.Select(x =>
                    Services.Games.TryGetGameDescriptor(x, out GameDescriptor? g) ? g.DisplayName.Value : x));
                await Services.MessageUI.DisplayMessageAsync(String.Format(Resources.ModLoader_GameTargetNotAddedError, gameTargetNames), MessageType.Error);
                return null;
            }

            // If there is more than 1 matching game we ask the user which one to patch
            if (gameInstallations.Count > 1)
            {
                GamesSelectionResult result = await Services.UI.SelectGamesAsync(new GamesSelectionViewModel(gameInstallations)
                {
                    Title = Resources.ModLoader_SelectInstallTargetTitle
                });

                if (result.CanceledByUser)
                    return null;

                gameInstallation = result.SelectedGame;
            }
            else
            {
                gameInstallation = gameInstallations.First();
            }
        }

        return new ModLoaderViewModel(gameInstallation, modFilePaths);
    }

    #endregion

    #region Public Methods

    public async Task<bool> InitializeAsync()
    {
        // Reset properties
        HasChanges = false;
        HasReorderedMods = false;
        ChangedModsText = null;
        SelectedMod = null;

        // Make sure we have write access
        if (!Services.File.CheckDirectoryWriteAccess(GameInstallation.InstallLocation.Directory))
        {
            Logger.Info("User does not have write access to game installation");
            await Services.MessageUI.DisplayMessageAsync(Resources.ModLoader_WriteAccessDenied, MessageType.Error);
            return false;
        }

        try
        {
            Library.VerifyLibrary();
        }
        catch (UnsupportedModLibraryFormatException ex)
        {
            Logger.Warn(ex, "Verifying library");
            await Services.MessageUI.DisplayMessageAsync(Resources.ModLoader_ReadLibraryVersionError, MessageType.Error);
            return false;
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Verifying library");

            await Services.MessageUI.DisplayExceptionMessageAsync(ex, Resources.ModLoader_ReadLibraryError);

            return false;
        }

        // Migrate legacy patches first
        await MigrateLegacyPatchesAsync();

        // Load mods
        bool success = await LoadInstalledModsAsync();

        if (!success)
            return false;

        // Check for mod updates if set to do so
        if (Services.Data.ModLoader_AutomaticallyCheckForUpdates)
            CheckForUpdatesCommand.Execute(null);

        List<Task> loadTasks = new();

        // Install pending mods
        if (_pendingModFiles != null)
        {
            foreach (ModToInstall modToInstall in _pendingModFiles)
            {
                DownloadableModsSource? source = DownloadableModsSource.GetSource(modToInstall.SourceId);
                loadTasks.Add(InstallModFromLocalFileAsync(source, modToInstall.FilePath, modToInstall.InstallData, modToInstall.FilePath.Name));
            }

            _pendingModFiles = null;
        }

        // Load downloadable mods
        loadTasks.Add(DownloadableMods.InitializeAsync());

        // Optional custom initialization
        if (_customInitAction != null)
        {
            loadTasks.Add(_customInitAction(this));
            _customInitAction = null;
        }

        // Wait for all loading tasks to finish
        await Task.WhenAll(loadTasks);

        Logger.Info("Finished initializing mod loader");

        return true;
    }

    public void RefreshDependencies()
    {
        foreach (ModViewModel mod in Mods)
        {
            if (mod.IsDownloaded)
                mod.DownloadedMod.UpdateDependencies(Mods);
        }
    }

    public void RefreshModifiedFiles()
    {
        Logger.Trace("Refreshing modified files");

        lock (Mods)
            ModifiedFiles.Refresh(Mods.Where(x => x.IsEnabled));

        AddedFilesText = ModifiedFiles.AddedFilesCount == 0 
            ? null 
            : new ResourceLocString(nameof(Resources.ModLoader_AddedFilesInfo), ModifiedFiles.AddedFilesCount);
        RemovedFilesText = ModifiedFiles.RemovedFilesCount == 0
            ? null
            : new ResourceLocString(nameof(Resources.ModLoader_RemovedFilesInfo), ModifiedFiles.RemovedFilesCount);
        PatchedFilesText = ModifiedFiles.PatchedFilesCount == 0
            ? null
            : new ResourceLocString(nameof(Resources.ModLoader_PatchedFilesInfo), ModifiedFiles.PatchedFilesCount);
    }

    public void ReportNewChanges()
    {
        lock (Mods)
        {
            Logger.Trace("New mod loader changes have been reported");

            // Remove mods marked as pending uninstall if they were not previously installed
            if (ModManifest != null)
            {
                foreach (ModViewModel mod in Mods.ToList())
                {
                    if (mod.InstallState == ModViewModel.ModInstallState.PendingUninstall &&
                        !ModManifest.Mods.ContainsKey(mod.DownloadedMod!.Metadata.Id))
                    {
                        mod.Dispose();
                        Mods.Remove(mod);
                        Services.Messenger.Send(new RemovedModFromLibraryMessage(GameInstallation, mod.DownloadableModsSource?.Id, mod.InstallData));
                    }
                }
            }

            RefreshDependencies();
            RefreshModifiedFiles();

            int changedMods = Mods.Count(x => x.HasChangesToApply);

            HasChanges = changedMods > 0 || HasReorderedMods;

            if (changedMods > 0)
                ChangedModsText = new ResourceLocString(nameof(Resources.ModLoader_UnsavedChangesInfo), changedMods);
            else if (HasReorderedMods)
                ChangedModsText = new ResourceLocString(nameof(Resources.ModLoader_UnsavedOrderChangesInfo));
            else
                ChangedModsText = null;
        }
    }

    public async Task InstallModFromFileAsync()
    {
        FileBrowserResult result = await Services.BrowseUI.BrowseFileAsync(new FileBrowserViewModel
        {
            Title = Resources.ModLoader_InstallSelectionTitle,
            ExtensionFilter = new FileFilterItem(_modExtractors.Select(x => x.FileExtension), Resources.ModLoader_ModFileTypeFilterName).StringRepresentation,
            MultiSelection = true
        });

        if (result.CanceledByUser)
            return;

        Logger.Info("Adding {0} mods to be installed", result.SelectedFiles.Length);

        await Task.WhenAll(result.SelectedFiles.Select(x => InstallModFromLocalFileAsync(null, x, null, x.Name)));

        Logger.Info("Added mods");
    }

    public async Task InstallModFromLocalFileAsync(
        DownloadableModsSource? source, 
        FileSystemPath filePath, 
        object? installData, 
        string? modName)
    {
        ModViewModel extractingMod = new(this, LoaderViewModel, source);
        extractingMod.InitExtracting(modName, installData);

        lock (Mods)
            Mods.Add(extractingMod);

        ModMetadata? pendingInstallModMetadata = null;
        try
        {
            CancellationToken cancellationToken = extractingMod.DownloadCancellationTokenSource?.Token ?? CancellationToken.None;

            pendingInstallModMetadata = await AddModToInstallAsync(
                filePath: filePath,
                progressCallback: extractingMod.SetProgress,
                cancellationToken: cancellationToken,
                sourceId: source?.Id,
                installData: installData,
                tempDownloadMod: extractingMod);

            if (pendingInstallModMetadata != null)
                ReportNewChanges();
        }
        catch (OperationCanceledException ex)
        {
            Logger.Info(ex, "Canceled extracting local mod");
        }
        catch (UnsupportedModVersionException ex)
        {
            Logger.Warn(ex, "Extracting local mod");

            await Services.MessageUI.DisplayExceptionMessageAsync(ex, String.Format(Resources.ModLoader_UnsupportedModVersionError, filePath.Name));
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Extracting local mod");

            await Services.MessageUI.DisplayExceptionMessageAsync(ex, String.Format(Resources.ModLoader_InstallError, filePath.Name));
        }

        lock (Mods)
        {
            // Remove the downloading mod if the download wasn't successful
            if (pendingInstallModMetadata == null)
            {
                extractingMod.Dispose();
                Mods.Remove(extractingMod);
            }
        }
    }

    public async Task InstallModFromDownloadableFileAsync(
        DownloadableModsSource? source, 
        ModViewModel? existingMod,
        string fileName, 
        string downloadUrl, 
        long? fileSize, 
        object? installData,
        string? modName)
    {
        if (existingMod is { IsDownloaded: false })
            throw new Exception("Can't install a mod to an existing mod which has not been downloaded");

        ModViewModel downloadingMod = new(this, LoaderViewModel, source);
        downloadingMod.InitDownloading(modName, installData);

        lock (Mods)
        {
            // The mod is being added as a new mod
            if (existingMod == null)
            {
                Mods.Add(downloadingMod);
            }
            // The mod is replacing an existing mod
            else
            {
                Mods[Mods.IndexOf(existingMod)] = downloadingMod;
            }
        }

        ModMetadata? pendingInstallModMetadata = null;
        try
        {
            Logger.Info("Downloading mod from {0}", downloadUrl);

            CancellationToken cancellationToken = downloadingMod.DownloadCancellationTokenSource?.Token ?? CancellationToken.None;

            // Create a temp file to download to
            using TempFile tempFile = new(false, new FileExtension(fileName));

            // Open a stream to the downloadable file
            using (Stream httpStream = await _httpClient.GetStreamAsync(downloadUrl))
            {
                // Download to the temp file
                using FileStream tempFileStream = File.Create(tempFile.TempPath);
                await httpStream.CopyToExAsync(
                    destination: tempFileStream,
                    progressCallback: downloadingMod.SetProgress,
                    cancellationToken: cancellationToken,
                    length: fileSize);
            }

            downloadingMod.SetInstallState(ModViewModel.ModInstallState.Extracting);

            pendingInstallModMetadata = await AddModToInstallAsync(
                filePath: tempFile.TempPath, 
                progressCallback: downloadingMod.SetProgress, 
                cancellationToken: cancellationToken, 
                sourceId: source?.Id, 
                installData: installData,
                tempDownloadMod: downloadingMod,
                existingMod: existingMod);

            if (pendingInstallModMetadata != null)
                ReportNewChanges();
        }
        catch (OperationCanceledException ex)
        {
            Logger.Info(ex, "Canceled downloading mod");
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Downloading mod");

            await Services.MessageUI.DisplayExceptionMessageAsync(ex, String.Format(Resources.ModLoader_DownloadModError, modName));
        }

        lock (Mods)
        {
            // Remove the downloading mod if the download wasn't successful
            if (pendingInstallModMetadata == null)
            {
                downloadingMod.Dispose();

                if (existingMod == null)
                    Mods.Remove(downloadingMod);
                else
                    Mods[Mods.IndexOf(downloadingMod)] = existingMod;

                Services.Messenger.Send(new RemovedModFromLibraryMessage(GameInstallation, source?.Id, installData));
            }
            // Restore the existing mod if the new mod has a different id
            else if (existingMod != null && pendingInstallModMetadata.Id != existingMod.DownloadedMod.Metadata.Id)
            {
                Mods.Add(existingMod);

                // Mark the mod as being uninstalled. Ideally the new downloaded mod should replace this,
                // but if it happens to have a separate id we want to remove this old mod.
                existingMod.SetToPendingUninstall();
            }
            // Dispose the existing mod since it's now removed
            else
            {
                existingMod?.Dispose();
            }
        }
    }

    public async Task CheckForUpdatesAsync()
    {
        Logger.Info("Checking all mods for updates");

        List<Task> updateTasks;
        lock (Mods)
            updateTasks = Mods.Select(x => x.CheckForUpdateAsync(_httpClient)).ToList();

        await Task.WhenAll(updateTasks);
        
        Logger.Info("Finished checking for updates");
    }

    public IReadOnlyCollection<ModViewModel> GetMods()
    {
        lock (Mods)
            return Mods.ToList();
    }

    public async Task<bool?> ApplyAsync()
    {
        IReadOnlyCollection<ModViewModel> mods = GetMods();

        if (mods.Any(x => !x.IsDownloaded))
        {
            await Services.MessageUI.DisplayMessageAsync(Resources.ModLoader_ActiveDownloadsError, Resources.ModLoader_ActiveDownloadsErrorHeader, MessageType.Warning);
            return null;
        }

        // Warn about missing dependencies
        foreach (ModViewModel mod in mods)
        {
            if (mod.IsDownloaded &&
                mod.InstallState != ModViewModel.ModInstallState.PendingUninstall &&
                mod.IsEnabled &&
                mod.DownloadedMod.Metadata.Dependencies != null)
            {
                List<string> missingMods = new();
                foreach (DownloadedModViewModel.ModDependencyViewModel? dependency in mod.DownloadedMod.Dependencies)
                {
                    if (dependency.State != DownloadedModViewModel.ModDependencyViewModel.DependencyState.Enabled)
                        missingMods.Add(dependency.Name);
                }

                if (missingMods.Any())
                {
                    bool result = await Services.MessageUI.DisplayMessageAsync(
                        String.Format(
                            Resources.ModLoader_MissingDependenciesWarning,
                            mod.Name, missingMods.Select(x => $"- {x}").JoinItems(Environment.NewLine)), MessageType.Warning, true);

                    if (!result)
                        return null;
                }
            }
        }

        // Warn about file conflicts
        if (ModifiedFiles.HasConflicts && Services.Data.ModLoader_ShowModConflictsWarning)
        {
            bool result = await Services.MessageUI.DisplayMessageAsync(Resources.ModLoader_FileConflictsWarning, Resources.ModLoader_FileConflictsWarningHeader, MessageType.Warning, true);

            if (!result)
                return null;
        }

        // Verify the game is not running
        if (Services.RunningGamesManager.IsGameRunning(GameInstallation))
        {
            await Services.MessageUI.DisplayMessageAsync(Resources.ModLoader_GameRunningError, Resources.ModLoader_GameRunningErrorHeader, MessageType.Error);
            return null;
        }

        // Verify the user has enough space left
        try
        {
            // Get the largest size for an archive to repack
            long minBytes = GetLargestArchiveSize(mods);

            // Add at least 1 GB for safety
            minBytes += 0x40000000;

            // Get the drive which has the user's temp folder
            DriveInfo driveInfo = new(new DirectoryInfo(Path.GetTempPath()).Root.Name);

            Logger.Info("Mod loader requires {0} bytes to apply mods", minBytes);

            // Check the available space
            if (driveInfo.AvailableFreeSpace < minBytes)
            {
                await Services.MessageUI.DisplayMessageAsync(
                    String.Format(Resources.ModLoader_InsufficientSpaceError, BinaryHelpers.BytesToString(minBytes)), 
                    Resources.ModLoader_InsufficientSpaceErrorHeader, 
                    MessageType.Error);
                return null;
            }
        }
        catch (Exception ex)
        {
            Logger.Warn(ex, "Checking available space on drive");
        }

        // Verify that a mod isn't trying to patch an exe with the SteamStub DRM
        if (GameInstallation.GetComponent<SteamGameClientComponent>()?.UsesSteamStubDrm == true &&
            GameInstallation.GameDescriptor.Structure is DirectoryProgramInstallationStructure structure)
        {
            string exeFilePath = structure.FileSystem.GetLocalPath(ProgramPathType.PrimaryExe);

            if (IsFilePatched(exeFilePath, mods))
            {
                try
                {
                    using FileStream exeFileStream = File.OpenRead(GameInstallation.InstallLocation.Directory + exeFilePath);
                    if (SteamHelpers.IsSteamStubDrmApplied(exeFileStream))
                    {
                        // TODO-LOC
                        await Services.MessageUI.DisplayMessageAsync(
                            $"At least one of the selected mods requires the game's executable file to have its DRM removed for it to be correctly patched.\n\nIn order to remove the DRM you can use a tool such as \"Steamless\". Then run it on the {Path.GetFileName(exeFilePath)} file and replace it with the unpacked file that it creates.",
                            "SteamStub DRM detected",
                            MessageType.Error);
                        return null;
                    }
                }
                catch (Exception ex)
                {
                    Logger.Warn(ex, "Checking for SteamStub DRM");   
                }
            }
        }

        using (LoaderLoadState state = await LoaderViewModel.RunAsync(Resources.ModLoader_ApplyStatus))
        {
            Logger.Info("Applying mods");

            try
            {
                await Task.Run(async () =>
                {
                    Dictionary<string, ModManifestEntry> installedMods = new();
                    
                    // Process every mod
                    foreach (ModViewModel modViewModel in mods)
                    {
                        if (!modViewModel.IsDownloaded)
                            throw new Exception("Mod has to be downloaded when applying");

                        string id = modViewModel.DownloadedMod.Metadata.Id;

                        switch (modViewModel.InstallState)
                        {
                            case ModViewModel.ModInstallState.Downloading:
                            case ModViewModel.ModInstallState.Extracting:
                                throw new Exception("Mod has to be downloaded when applying");

                            // Already installed mod
                            case ModViewModel.ModInstallState.Installed:
                                installedMods.Add(id, new ModManifestEntry(id, modViewModel.DownloadedMod.InstallInfo, modViewModel.IsEnabled));
                                break;
                            
                            // Install new mod
                            case ModViewModel.ModInstallState.PendingInstall:
                                Library.InstallMod(modViewModel.DownloadedMod.Mod.ModDirectoryPath, id, false);
                                installedMods.Add(id, new ModManifestEntry(id, modViewModel.DownloadedMod.InstallInfo, modViewModel.IsEnabled));
                                break;
                            
                            // Uninstall
                            case ModViewModel.ModInstallState.PendingUninstall:
                                Library.UninstallMod(modViewModel.DownloadedMod.Metadata.Id);
                                break;

                            default:
                                throw new InvalidOperationException($"Mod state {modViewModel.InstallState} is invalid");
                        }
                    }

                    // Update the mod manifest
                    Library.WriteModManifest(new ModManifest(installedMods));

                    // Apply the mods
                    ApplyModsResult result = await ModProcessor.ApplyAsync(Library, state.SetProgress);

                    Services.Messenger.Send(new ModifiedGameModsMessage(GameInstallation));

                    Logger.Info("Applied mods");

                    state.Complete();

                    if (result.Success)
                        await Services.MessageUI.DisplaySuccessfulActionMessageAsync(Resources.ModLoader_ApplySuccess);
                    else
                        await Services.MessageUI.DisplayMessageAsync(
                            String.Format(Resources.ModLoader_ApplyWithErrors, result.ErrorMessage), 
                            MessageType.Warning);
                });
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Applying mods");

                state.Error();
                
                await Services.MessageUI.DisplayExceptionMessageAsync(ex, Resources.ModLoader_ApplyError);
            }
        }

        // No matter if it succeeds or fails we want to reset the state
        return await InitializeAsync();
    }

    public void Dispose()
    {
        lock (Mods)
            Mods.DisposeAll();
        _httpClient.Dispose();
        DownloadableMods.Dispose();
    }

    #endregion

    #region Records

    public record ModToInstall(FileSystemPath FilePath, string? SourceId, object? InstallData);

    #endregion
}