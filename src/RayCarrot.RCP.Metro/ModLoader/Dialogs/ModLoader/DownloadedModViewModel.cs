using System.Windows.Input;
using System.Windows.Media;
using RayCarrot.RCP.Metro.ModLoader.Library;
using RayCarrot.RCP.Metro.ModLoader.Metadata;
using RayCarrot.RCP.Metro.ModLoader.Sources;

namespace RayCarrot.RCP.Metro.ModLoader.Dialogs.ModLoader;

public class DownloadedModViewModel : BaseViewModel
{
    #region Constructor

    public DownloadedModViewModel(GameInstallation gameInstallation, DownloadableModsSource? downloadableModsSource, Mod mod, ModManifestEntry modEntry)
    {
        GameInstallation = gameInstallation;
        DownloadableModsSource = downloadableModsSource;
        Mod = mod;
        InstallInfo = modEntry.InstallInfo;

        ModInfo = new ObservableCollection<DuoGridItemViewModel>()
        {
            new(new ResourceLocString(nameof(Resources.ModLoader_ModInfo_Author)), Metadata.Author),
            new(new ResourceLocString(nameof(Resources.ModLoader_ModInfo_Version)), Metadata.Version?.ToString()),
            new(new ResourceLocString(nameof(Resources.ModLoader_ModInfo_FormatVersion)), Metadata.Format.ToString(), UserLevel.Technical),
            new(new ResourceLocString(nameof(Resources.ModLoader_ModInfo_ID)), Metadata.Id, UserLevel.Technical),
            new(new ResourceLocString(nameof(Resources.ModLoader_ModInfo_Size)), BinaryHelpers.BytesToString(modEntry.InstallInfo.Size)),
            new(new ResourceLocString(nameof(Resources.ModLoader_ModInfo_InstallSource)), downloadableModsSource?.DisplayName ?? new ResourceLocString(nameof(Resources.ModLoader_LocalInstallSource))),
            new(new ResourceLocString(nameof(Resources.ModLoader_ModInfo_Modules)), Mod.GetSupportedModules().JoinItems(", ", x => x.Id)),
            new(new ResourceLocString(nameof(Resources.ModLoader_ModInfo_AddedFiles)), mod.GetAddedFiles().Count.ToString()),
            new(new ResourceLocString(nameof(Resources.ModLoader_ModInfo_RemovedFiles)), mod.GetRemovedFiles().Count.ToString()),
            new(new ResourceLocString(nameof(Resources.ModLoader_ModInfo_PatchedFiles)), mod.GetPatchedFiles().Count.ToString()),
        };

        ReadOnlyCollection<string> unsupportedModules = Mod.GetUnsupportedModules();
        if (unsupportedModules.Any())
            UnsupportedModulesErrorMessage = new ResourceLocString(nameof(Resources.ModLoader_UnsupportedModulesInfo), unsupportedModules.JoinItems(", "));

        ChangelogEntries = new ObservableCollection<ModChangelogEntry>(Metadata.Changelog ?? Array.Empty<ModChangelogEntry>());

        Dependencies = new ObservableCollection<ModDependencyViewModel>(Metadata.Dependencies?.OrderByDescending(x => x.Sort).Select(x => new ModDependencyViewModel(Metadata, x, gameInstallation)) ?? []);

        CanOpenInDownloadPage = downloadableModsSource != null;

        OpenWebsiteCommand = new AsyncRelayCommand(OpenWebsiteAsync);
        OpenInDownloadPageCommand = new RelayCommand(OpenInDownloadPage);
    }

    #endregion

    #region Logger

    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    #endregion

    #region Commands

    public ICommand OpenWebsiteCommand { get; }
    public ICommand OpenInDownloadPageCommand { get; }

    #endregion

    #region Public Properties

    public GameInstallation GameInstallation { get; }
    public DownloadableModsSource? DownloadableModsSource { get; }
    public Mod Mod { get; }
    public ModInstallInfo InstallInfo { get; }
    public ModMetadata Metadata => Mod.Metadata;
    public ObservableCollection<DuoGridItemViewModel> ModInfo { get; }
    public ObservableCollection<ModChangelogEntry> ChangelogEntries { get; }
    public ObservableCollection<ModDependencyViewModel> Dependencies { get; }
    public bool HasDependents { get; set; }

    public LocalizedString? UnsupportedModulesErrorMessage { get; }

    public string? Name => Metadata.Name;
    public string? Description => Metadata.Description;
    public string? Website => Metadata.Website;

    public bool HasWebsite => Uri.TryCreate(Metadata.Website, UriKind.Absolute, out _);
    public bool HasDescripton => !Metadata.Description.IsNullOrWhiteSpace();
    public ImageSource? Thumbnail { get; set; }

    public bool CanOpenInDownloadPage { get; }

    #endregion

    #region Public Methods

    public void LoadThumbnail()
    {
        Logger.Trace("Loading thumbnail for mod with ID {0}", Metadata.Id);

        try
        {
            FileSystemPath? thumbFilePath = Mod.GetThumbnailFilePath();

            if (thumbFilePath == null)
            {
                Thumbnail = null;
                return;
            }

            Thumbnail = BitmapImageHelpers.CreateFromFile(thumbFilePath);
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Loading mod thumbnail");
        }
    }

    public void UpdateDependencies(IList<ModViewModel> mods)
    {
        foreach (ModDependencyViewModel dependency in Dependencies)
        {
            ModViewModel? matchingMod = mods.FirstOrDefault(x => 
                x.IsDownloaded && 
                x.DownloadedMod != this &&
                x.InstallState != ModViewModel.ModInstallState.PendingUninstall && 
                dependency.ModDependency.Ids.Contains(x.DownloadedMod.Metadata.Id));

            dependency.UpdateState(mods, matchingMod);
        }

        HasDependents = mods.Any(x => x.IsDownloaded &&
                                      x.DownloadedMod != this &&
                                      x.InstallState != ModViewModel.ModInstallState.PendingUninstall &&
                                      x.DownloadedMod.Metadata.Dependencies?.Any(d => d.Ids.Contains(Metadata.Id)) == true);
    }

    public async Task OpenWebsiteAsync()
    {
        if (Metadata.Website != null)
        {
            if (await Services.MessageUI.DisplayMessageAsync(
                    String.Format(Resources.ExternalLinkWarning, Metadata.Website), 
                    Resources.ExternalLinkWarningHeader, MessageType.Question, true))
                Services.App.OpenUrl(Metadata.Website);
        }
    }

    public void OpenInDownloadPage()
    {
        if (DownloadableModsSource != null)
        {
            object? installData = DownloadableModsSource.ParseInstallData(InstallInfo.Data);
            Services.Messenger.Send(new OpenModDownloadPageMessage(GameInstallation, InstallInfo.Source, installData));
        }
    }

    #endregion

    #region Classes

    public class ModDependencyViewModel : BaseViewModel
    {
        public ModDependencyViewModel(ModMetadata dependantMetadata, ModDependencyInfo modDependency, GameInstallation gameInstallation)
        {
            DependantMetadata = dependantMetadata;
            ModDependency = modDependency;
            GameInstallation = gameInstallation;
            Name = modDependency.Name;
            State = DependencyState.NotDownloaded;

            OpenModCommand = new RelayCommand(OpenMod);
        }

        public ICommand OpenModCommand { get; }

        public GameInstallation GameInstallation { get; }
        public ModMetadata DependantMetadata { get; }
        public ModDependencyInfo ModDependency { get; }
        public string Name { get; set; }
        public DependencyState State { get; set; }

        public void UpdateState(IList<ModViewModel> mods, ModViewModel? mod)
        {
            if (mod is not { IsDownloaded: true })
            {
                Name = ModDependency.Name;
                State = DependencyState.NotDownloaded;
            }
            else
            {
                Name = mod.Name ?? ModDependency.Name;

                // Check if enabled
                if (!mod.IsEnabled)
                {
                    State = DependencyState.Disabled;
                }
                // Check sorting
                else if (ModDependency.Sort != 0 && DependantMetadata.Dependencies != null)
                {
                    int dependantModIndex = mods.FindItemIndex(x => x.IsDownloaded && x.DownloadedMod.Metadata.Id == DependantMetadata.Id);
                    if (dependantModIndex == -1)
                        throw new Exception("Couldn't find the dependant mod");

                    int modIndex = mods.IndexOf(mod);
                    if (modIndex == -1)
                        throw new Exception("Couldn't find the mod index");

                    // Check against dependant mod
                    if (-ModDependency.Sort > 0 && modIndex < dependantModIndex)
                    {
                        State = DependencyState.SortedTooHigh;
                        return;
                    }
                    else if (-ModDependency.Sort < 0 && modIndex > dependantModIndex)
                    {
                        State = DependencyState.SortedTooLow;
                        return;
                    }

                    // Check against other dependencies
                    foreach (ModDependencyInfo dependency in DependantMetadata.Dependencies)
                    {
                        if (dependency != ModDependency && dependency.Sort != 0)
                        {
                            int dependencyModIndex = mods.FindItemIndex(x =>
                                x.IsDownloaded &&
                                x.InstallState != ModViewModel.ModInstallState.PendingUninstall &&
                                dependency.Ids.Contains(x.DownloadedMod.Metadata.Id));

                            if (dependencyModIndex != -1)
                            {
                                if (-dependency.Sort > -ModDependency.Sort)
                                {
                                    if (dependencyModIndex < modIndex)
                                    {
                                        State = DependencyState.SortedTooLow;
                                        return;
                                    }
                                }
                                else if (-dependency.Sort < -ModDependency.Sort)
                                {
                                    if (dependencyModIndex > modIndex)
                                    {
                                        State = DependencyState.SortedTooHigh;
                                        return;
                                    }
                                }
                            }
                        }
                    }

                    State = DependencyState.Enabled;
                }
                else
                {
                    State = DependencyState.Enabled;
                }
            }
        }

        public void OpenMod()
        {
            DownloadableModsSource? source = DownloadableModsSource.GetSource(ModDependency.SourceId);

            object? installData = source?.ParseDependencyDataAsInstallData(ModDependency.SourceData);
            if (installData != null)
                Services.Messenger.Send(new OpenModDownloadPageMessage(GameInstallation, ModDependency.SourceId, installData));
        }

        public enum DependencyState
        {
            Enabled,
            Disabled,
            SortedTooHigh,
            SortedTooLow,
            NotDownloaded,
        }
    }

    #endregion
}