using System.Windows.Input;
using Nito.AsyncEx;
using RayCarrot.RCP.Metro.Pages.Settings.Sections;

namespace RayCarrot.RCP.Metro.Pages.Settings;

/// <summary>
/// View model for the settings page
/// </summary>
public class SettingsPageViewModel : BasePageViewModel
{
    #region Constructor

    public SettingsPageViewModel(
        AppViewModel app, 
        AppUserData data, 
        IMessageUIManager messageUi, 
        AppUIManager ui, 
        JumpListManager jumpListManager, 
        FileManager fileManager,
        AssociatedFileEditorsManager associatedFileEditorsManager,
        IMessenger messenger,
        DiscordManager discordManager,
        RunningGamesManager runningGamesManager) : base(app)
    {
        // Set services
        Data = data ?? throw new ArgumentNullException(nameof(data));
        MessageUI = messageUi ?? throw new ArgumentNullException(nameof(messageUi));

        // Create commands
        RefreshCommand = new AsyncRelayCommand(RefreshAsync);
        ResetCommand = new AsyncRelayCommand(ResetAsync);

        // Create properties
        AsyncLock = new AsyncLock();

        // Create sections
        Sections = new ObservableCollection<ObservableCollection<SettingsSectionViewModel>>()
        {
            new() { new LanguageSettingsSectionViewModel(Data, App), new UserLevelSettingsSectionViewModel(Data), },
            new() { new GeneralSettingsSectionViewModel(Data, ui, jumpListManager, discordManager, runningGamesManager), },
            new() { new DesignSettingsSectionViewModel(Data), },
            new() { new StartupSettingsSectionViewModel(Data), },
            new() { new FilesSettingsSectionViewModel(Data, ui, messageUi, associatedFileEditorsManager, messenger), },
            new() { new WindowsIntegrationSettingsSectionViewModel(Data, MessageUI), },
            new() { new ProgressionSettingsSectionViewModel(Data), },
            new() { new ArchiveExplorerSettingsSectionViewModel(Data), },
            new() { new ModLoaderSettingsSectionViewModel(Data), },
            new() { new DebugSettingsSectionViewModel(Data, fileManager), },
        };
        FlatSections = new ObservableCollection<SettingsSectionViewModel>(Sections.SelectMany(x => x));
    }

    #endregion

    #region Commands

    public ICommand RefreshCommand { get; }
    public ICommand ResetCommand { get; }

    #endregion

    #region Private Properties

    /// <summary>
    /// The async lock for refreshing the links
    /// </summary>
    private AsyncLock AsyncLock { get; }

    #endregion

    #region Services

    public AppUserData Data { get; }
    public IMessageUIManager MessageUI { get; }

    #endregion

    #region Public Properties

    public override AppPage Page => AppPage.Settings;

    public ObservableCollection<ObservableCollection<SettingsSectionViewModel>> Sections { get; }
    public ObservableCollection<SettingsSectionViewModel> FlatSections { get; }

    #endregion

    #region Public Methods

    protected override async Task InitializeAsync()
    {
        await RefreshAsync();
    }

    /// <summary>
    /// Refreshes the page
    /// </summary>
    /// <returns>The task</returns>
    public async Task RefreshAsync()
    {
        using (await AsyncLock.LockAsync())
        {
            // Refresh the sections
            foreach (SettingsSectionViewModel section in FlatSections)
                section.Refresh();
        }
    }

    public async Task ResetAsync()
    {
        if (!await MessageUI.DisplayMessageAsync(Resources.ConfirmResetData, Resources.ConfirmResetDataHeader, MessageType.Question, true))
        {
            return;
        }

        // The app data can't be reset while the app is running as it could cause multiple issues, so better to restart
        await App.RestartAsync(asAdmin: false, "-reset");
    }

    #endregion
}