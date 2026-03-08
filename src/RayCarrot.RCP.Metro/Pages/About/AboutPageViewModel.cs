using System.Reflection;
using System.Windows.Input;

namespace RayCarrot.RCP.Metro.Pages.About;

/// <summary>
/// View model for the about page
/// </summary>
public class AboutPageViewModel : BasePageViewModel
{
    #region Constructor

    /// <summary>
    /// Default constructor
    /// </summary>
    public AboutPageViewModel(
        AppViewModel app, 
        AppUserData data, 
        AppUIManager ui, 
        IMessageUIManager messageUi,
        FileManager file, 
        DeployableFilesManager deployableFiles) : base(app)
    {
        // Set services
        Data = data ?? throw new ArgumentNullException(nameof(data));
        UI = ui ?? throw new ArgumentNullException(nameof(ui));
        MessageUI = messageUi ?? throw new ArgumentNullException(nameof(messageUi));
        File = file ?? throw new ArgumentNullException(nameof(file));
        DeployableFiles = deployableFiles ?? throw new ArgumentNullException(nameof(deployableFiles));

        // Set the credits
        Credits = new ObservableCollection<DuoGridItemViewModel>()
        {
            new("RayCarrot", new ResourceLocString(nameof(Resources.About_Credits_RayCarrot))),
            new("Clef", new ResourceLocString(nameof(Resources.About_Credits_Clef))),
            new("Dr_st", new ResourceLocString(nameof(Resources.About_Credits_Dr_st))),
            new("Droolie", new ResourceLocString(nameof(Resources.About_Credits_Droolie))),
            new("Fabiosek", new ResourceLocString(nameof(Resources.About_Credits_Fabiosek))),
            new("Haruka_VII", new ResourceLocString(nameof(Resources.About_Credits_Haruka_VII))),
            new("ItzalDrake", new ResourceLocString(nameof(Resources.About_Credits_ItzalDrake))),
            new("Juanmv94", new ResourceLocString(nameof(Resources.About_Credits_Janmv94))),
            new("Lex", new ResourceLocString(nameof(Resources.About_Credits_Lex))),
            new("Mark", new ResourceLocString(nameof(Resources.About_Credits_432Hz))),
            new("Marcos03BR", new ResourceLocString(nameof(Resources.About_Credits_Marcos03BR))),
            new("Marteaufou", new ResourceLocString(nameof(Resources.About_Credits_Marteaufou))),
            new("Mr5088", new ResourceLocString(nameof(Resources.About_Credits_Mr5088))),
            new("Nic", new ResourceLocString(nameof(Resources.About_Credits_Nic))),
            new("Noserdog", new ResourceLocString(nameof(Resources.About_Credits_Noserdog))),
            new("PluMGMK", new ResourceLocString(nameof(Resources.About_Credits_PluMGMK))),
            new("Rayman Universe", new ResourceLocString(nameof(Resources.About_Credits_RaymanUniverse))),
            new("RibShark", new ResourceLocString(nameof(Resources.About_Credits_RibShark))),
            new("Rorias", new ResourceLocString(nameof(Resources.About_Credits_Rorias))),
            new("Snagglebee", new ResourceLocString(nameof(Resources.About_Credits_Snagglebee))),
            new("XanderNT", new ResourceLocString(nameof(Resources.About_Credits_XanderNT))),
            new("ZetaXO", new ResourceLocString(nameof(Resources.About_Credits_ZetaXO))),
        };

        // Create commands
        OpenUrlCommand = new RelayCommand(x =>
        {
            if (x is not null)
                App.OpenUrl(x.ToString());
        });
        ShowVersionHistoryCommand = new AsyncRelayCommand(ShowVersionHistoryAsync);
        CheckForUpdatesCommand = new AsyncRelayCommand(async () => await App.CheckForUpdatesAsync(true));
        UninstallCommand = new AsyncRelayCommand(UninstallAsync);

        // Refresh the update badge property based on if new update is available
        Data.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(Data.Update_IsUpdateAvailable))
                OnPropertyChanged(nameof(UpdateBadge));
        };
    }

    #endregion

    #region Logger

    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    #endregion

    #region Commands

    public ICommand OpenUrlCommand { get; }
    public ICommand ShowVersionHistoryCommand { get; }
    public ICommand CheckForUpdatesCommand { get; }
    public ICommand UninstallCommand { get; }

    #endregion

    #region Services

    private AppUserData Data { get; }
    private AppUIManager UI { get; }
    private IMessageUIManager MessageUI { get; }
    private FileManager File { get; }
    private DeployableFilesManager DeployableFiles { get; }

    #endregion

    #region Public Properties

    public override AppPage Page => AppPage.About;

    public ObservableCollection<DuoGridItemViewModel> Credits { get; }

    /// <summary>
    /// The update badge, indicating if new updates are available
    /// </summary>
    public string? UpdateBadge => Data.Update_IsUpdateAvailable ? "1" : null;

    #endregion

    #region Public Methods

    /// <summary>
    /// Shows the application version history
    /// </summary>
    public Task ShowVersionHistoryAsync() => UI.ShowVersionHistoryAsync();

    /// <summary>
    /// Runs the uninstaller
    /// </summary>
    /// <returns>The task</returns>
    public async Task UninstallAsync()
    {
        // Confirm
        if (!await MessageUI.DisplayMessageAsync(Resources.About_ConfirmUninstall, Resources.About_ConfirmUninstallHeader, MessageType.Question, true))
            return;

        FileSystemPath filePath;

        try
        {
            filePath = DeployableFiles.DeployFile(DeployableFilesManager.DeployableFile.Uninstaller);
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Deploying uninstaller");
            await MessageUI.DisplayExceptionMessageAsync(ex, Resources.DeployFilesError);
            return;
        }

        // Run the uninstaller
        if (await File.LaunchFileAsync(filePath, true, $"\"{Assembly.GetEntryAssembly()?.Location}\"") == null)
        {
            string[] appDataLocations = 
            {
                AppFilePaths.UserDataBaseDir,
                AppFilePaths.RegistryBaseKey
            };

            await MessageUI.DisplayMessageAsync(String.Format(Resources.About_UninstallFailed, appDataLocations.JoinItems(Environment.NewLine)), MessageType.Error);

            return;
        }

        // Shut down the app
        await Metro.App.Current.ShutdownAppAsync(true);
    }

    #endregion
}