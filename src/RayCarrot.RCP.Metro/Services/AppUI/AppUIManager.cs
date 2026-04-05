#nullable disable
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Media;
using System.Windows.Threading;
using RayCarrot.RCP.Metro.Archive;
using RayCarrot.RCP.Metro.Games.Clients;
using RayCarrot.RCP.Metro.Games.Components;
using RayCarrot.RCP.Metro.Games.Settings;
using RayCarrot.RCP.Metro.Games.Tools.PrototypeRestoration;
using RayCarrot.RCP.Metro.Games.Tools.RuntimeModifications;
using RayCarrot.RCP.Metro.ModLoader.Dialogs.ModCreator;
using RayCarrot.RCP.Metro.ModLoader.Dialogs.ModLoader;

namespace RayCarrot.RCP.Metro;

/// <summary>
/// The UI Manager of this application
/// </summary>
public class AppUIManager
{
    #region Constructor

    public AppUIManager(IDialogBaseManager dialog)
    {
        Dialog = dialog ?? throw new ArgumentNullException(nameof(dialog));
    }

    #endregion

    #region Logger

    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    #endregion

    #region Private Properties

    private IDialogBaseManager Dialog { get; }

    #endregion

    #region Private Methods

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private Dispatcher GetDispatcher()
    {
        return Application.Current.Dispatcher ?? throw new Exception("The application does not have a valid dispatcher");
    }

    private async Task<Result> ShowDialogAsync<UserInput, Result>(Func<IDialogWindowControl<UserInput, Result>> createDialogFunc)
        where UserInput : UserInputViewModel
        where Result : UserInputResult
    {
        Dispatcher dispatcher = GetDispatcher();

        // Create the dialog on the UI thread
        IDialogWindowControl<UserInput, Result> dialog = dispatcher.Invoke(createDialogFunc);
        string dialogTypeName = dialog.GetType().Name;

        Logger.Trace("A dialog of type {0} was opened", dialogTypeName);

        // Show the dialog
        await Dialog.ShowWindowAsync(dialog, ShowWindowFlags.Modal, title: dialog.ViewModel.Title);

        // Get the result
        Result result = dispatcher.Invoke(dialog.GetResult);

        if (result == null)
            Logger.Warn("The dialog of type {0} returned null", dialogTypeName);
        else if (result.CanceledByUser)
            Logger.Trace("The dialog of type {0} was canceled by the user", dialogTypeName);

        // Return the result
        return result;
    }

    private async Task<ShowWindowResult> ShowWindowAsync(
        Func<IWindowControl> createWindowFunc, 
        ShowWindowFlags flags = ShowWindowFlags.None,
        string[] typeGroupNames = null,
        string[] globalGroupNames = null)
    {
        Dispatcher dispatcher = GetDispatcher();

        IWindowControl window = dispatcher.Invoke(createWindowFunc);
        string windowTypeName = window.GetType().Name;

        Logger.Trace("A window of type {0} was opened", windowTypeName);

        return await Dialog.ShowWindowAsync(window, flags, typeGroupNames, globalGroupNames);
    }

    #endregion

    #region Dialogs

    public Task<JumpListEditResult> EditJumpListAsync(JumpListEditViewModel viewModel) => ShowDialogAsync(() => new JumpListEditDialog(viewModel));

    public async Task<ItemSelectionDialogResult> SelectItemAsync(ItemSelectionDialogViewModel viewModel)
    {
        // If only one item is available, return it
        if (viewModel.Items.Length == 1)
        {
            return new ItemSelectionDialogResult()
            {
                CanceledByUser = false,
                SelectedIndex = 0
            };
        }

        return await ShowDialogAsync(() => new ItemSelectionDialog(viewModel));
    }

    public Task<StringInputResult> GetStringInputAsync(StringInputViewModel stringInputViewModel) => ShowDialogAsync(() => new StringInputDialog(stringInputViewModel));

    public Task<ProgramSelectionResult> GetProgramAsync(ProgramSelectionViewModel programSelectionViewModel) => ShowDialogAsync(() => new ProgramSelectionDialog(programSelectionViewModel));

    public Task<GamesSelectionResult> SelectGamesAsync(GamesSelectionViewModel gamesSelectionViewModel) => 
        ShowDialogAsync(() => new GamesSelectionDialog(gamesSelectionViewModel));

    public Task<ColorSelectionResult> SelectColorAsync(ColorSelectionViewModel colorSelectionViewModel) => 
        ShowDialogAsync(() => new ColorSelectionDialog(colorSelectionViewModel));

    /// <summary>
    /// Allows the user to browse for a drive
    /// </summary>
    /// <param name="driveBrowserModel">The drive browser information</param>
    /// <returns>The browse drive result</returns>
    public Task<DriveBrowserResult> BrowseDriveAsync(DriveBrowserViewModel driveBrowserModel) => ShowDialogAsync(() => new DriveSelectionDialog(driveBrowserModel));

    public Task<GameInstallerResult> InstallGameAsync(GameDescriptor gameDescriptor, GameInstallerInfo info) => 
        ShowDialogAsync(() => new GameInstallerDialog(gameDescriptor, info));

    /// <summary>
    /// Shows a new instance of the update available dialog
    /// </summary>
    /// <param name="updaterCheckResult">The update check result for the new update</param>
    /// <returns>Indicates if the new update should be installed</returns>
    public async Task<bool> ShowUpdateAvailableAsync(UpdaterCheckResult updaterCheckResult)
    {
        UpdateAvailableDialogViewModel viewModel = new(updaterCheckResult);
        await ShowWindowAsync(() => new UpdateAvailableDialog(viewModel), ShowWindowFlags.Modal);
        return viewModel.RequestedInstallNewUpdate;
    }

    /// <summary>
    /// Displays a message to the user
    /// </summary>
    /// <param name="message">The message to display</param>
    /// <param name="header">The header for the message</param>
    /// <param name="messageType">The type of message, determining its visual appearance</param>
    /// <param name="allowCancel">True if the option to cancel is present</param>
    /// <param name="additionalActions">Additional actions</param>
    /// <returns>True if the user accepted the message, otherwise false</returns>
    public async Task<bool> DisplayMessageAsync(string message, string header, MessageType messageType, bool allowCancel, IList<DialogMessageActionViewModel> additionalActions)
    {
        GetDispatcher();

        Logger.Trace("A message of type {0} was displayed with the content of: '{1}'", messageType, message);

        // Get the header message to use
        var headerMessage = !header.IsNullOrWhiteSpace()
            ? header
            // If we don't have a message, use the default one for the message type
            : messageType switch
            {
                MessageType.Generic => Resources.MessageHeader_Generic,
                MessageType.Information => Resources.MessageHeader_Information,
                MessageType.Error => Resources.MessageHeader_Error,
                MessageType.Warning => Resources.MessageHeader_Warning,
                MessageType.Success => Resources.MessageHeader_Success,
                MessageType.Question => Resources.MessageHeader_Question,
                _ => Resources.MessageHeader_Generic
            };

        // Helper method for getting the image source for the message type
        static string GetImgSource(MessageType mt)
        {
            MessageIconAsset asset = mt switch
            {
                MessageType.Generic => MessageIconAsset.Generic,
                MessageType.Information => MessageIconAsset.Generic,
                MessageType.Error => MessageIconAsset.Error,
                MessageType.Warning => MessageIconAsset.Info,
                MessageType.Question => MessageIconAsset.Question,
                MessageType.Success => MessageIconAsset.Happy,
                _ => MessageIconAsset.Generic
            };

            return asset.GetAssetPath();
        }

        // Create the message actions
        var actions = new List<DialogMessageActionViewModel>();

        // Create a cancel action if available
        if (allowCancel)
            actions.Add(new DialogMessageActionViewModel()
            {
                DisplayText = Resources.Cancel,
                DisplayDescription = Resources.Cancel,
                IsCancel = true,
                ActionResult = new UserInputResult(),
            });

        // Add additional actions
        actions.AddRange(additionalActions);

        // Create the default action
        actions.Add(new DialogMessageActionViewModel()
        {
            DisplayText = Resources.Ok,
            DisplayDescription = Resources.Ok,
            IsDefault = true,
            ActionResult = new UserInputResult()
            {
                CanceledByUser = false
            },
        });

        // Run on the UI thread
        return await Application.Current.Dispatcher.Invoke(async () =>
        {
            // Create the view model
            var vm = new DialogMessageViewModel()
            {
                MessageText = message,
                Title = headerMessage,
                MessageType = messageType,
                DialogImageSource = (ImageSource)new ImageSourceConverter().ConvertFromString(GetImgSource(messageType)),
                DialogActions = actions,
                DefaultActionResult = new UserInputResult(),
            };

            // Create the message box
            var dialog = new DialogMessageBox(vm);

            // Show the dialog
            await Dialog.ShowWindowAsync(dialog, ShowWindowFlags.Modal, title: vm.Title);

            // Get the result
            UserInputResult result = dialog.GetResult();

            // Return the result
            return !result.CanceledByUser;
        });
    }

    #endregion

    #region Windows

#nullable enable

    /// <summary>
    /// Shows a new instance of the version history
    /// </summary>
    /// <returns>The task</returns>
    public async Task ShowVersionHistoryAsync() =>
        await ShowWindowAsync(() => new VersionHistoryDialog(), ShowWindowFlags.Modal);

    /// <summary>
    /// Shows a new instance of the game settings
    /// </summary>
    /// <param name="gameInstallation">The game installation to show the settings for</param>
    /// <returns>The task</returns>
    public async Task ShowGameSettingsAsync(GameInstallation gameInstallation)
    {
        await ShowWindowAsync(
            () => new GameSettingsDialog(gameInstallation.GetRequiredComponent<GameSettingsComponent>().CreateObject()),
            // Only allow one settings window per installation
            typeGroupNames: new[] { gameInstallation.InstallationId });
    }

    /// <summary>
    /// Shows a new instance of the game client game settings
    /// </summary>
    /// <param name="gameInstallation">The game installation to show the settings for</param>
    /// <returns>The task</returns>
    public async Task ShowGameClientGameOptionsAsync(GameInstallation gameInstallation)
    {
        await ShowWindowAsync(
            () => new GameSettingsDialog(gameInstallation.GetRequiredComponent<GameClientGameSettingsComponent>().CreateObject()), 
            // Only allow one settings window per installation
            typeGroupNames: new[] { gameInstallation.InstallationId });
    }

    /// <summary>
    /// Shows a new instance of the game debug
    /// </summary>
    /// <param name="gameInstallation">The game installation to show the debug for</param>
    /// <returns>The task</returns>
    public async Task ShowGameDebugAsync(GameInstallation gameInstallation) =>
        await ShowWindowAsync(() => new GameDebugDialog(gameInstallation), ShowWindowFlags.DuplicateTypesNotAllowed);

    /// <summary>
    /// Shows a new instance of the game client debug
    /// </summary>
    /// <param name="gameClientInstallation">The game client installation to show the debug for</param>
    /// <returns>The task</returns>
    public async Task ShowGameClientDebugAsync(GameClientInstallation gameClientInstallation) =>
        await ShowWindowAsync(() => new GameClientDebugDialog(gameClientInstallation), ShowWindowFlags.DuplicateTypesNotAllowed);

    /// <summary>
    /// Shows a new instance of the Archive Explorer
    /// </summary>
    /// <param name="manager">The archive data manager</param>
    /// <param name="filePaths">The archive file paths</param>
    /// <returns>The task</returns>
    public async Task ShowArchiveExplorerAsync(IArchiveDataManager manager, FileSystemPath[] filePaths) =>
        await ShowWindowAsync(() => new ArchiveExplorerDialog(new ArchiveExplorerDialogViewModel(manager, filePaths)));

    /// <summary>
    /// Shows a new instance of the Archive Creator
    /// </summary>
    /// <param name="manager">The archive data manager</param>
    /// <returns>The task</returns>
    public async Task ShowArchiveCreatorAsync(IArchiveDataManager manager) =>
        await ShowWindowAsync(() => new ArchiveCreatorDialog(new ArchiveCreatorDialogViewModel(manager)));

    /// <summary>
    /// Shows a new instance of the Mod Loader from a game installation
    /// </summary>
    /// <param name="gameInstallation">The game installation</param>
    /// <param name="customInitAction">A custom action to run after initializing the mod loader</param>
    /// <returns>The task</returns>
    public async Task ShowModLoaderAsync(GameInstallation gameInstallation, Func<ModLoaderViewModel, Task>? customInitAction = null)
    {
        ShowWindowResult result = await ShowWindowAsync(
            () => new ModLoaderDialog(new ModLoaderViewModel(gameInstallation, customInitAction: customInitAction)),
            // Only allow one Mod Loader window per installation
            typeGroupNames: new[] { gameInstallation.InstallationId });

        // If we failed to open the window we try and run the custom init action in the blocking block loader instance
        if (customInitAction != null &&
            !result.Success && 
            result.BlockingWindowInstance is ModLoaderDialog modLoader &&
            !modLoader.ViewModel.LoaderViewModel.IsRunning &&
            modLoader.ViewModel.GameInstallation == gameInstallation)
        {
            await customInitAction(modLoader.ViewModel);
        }
    }

    /// <summary>
    /// Shows a new instance of the Mod Loader from mod file paths
    /// </summary>
    /// <param name="gameInstallation">The game installation to use the mod loader for. If not specified then the game is picked from what the mod supports.</param>
    /// <param name="modPaths">The mods to install</param>
    /// <returns>The task</returns>
    public async Task ShowModLoaderAsync(GameInstallation? gameInstallation, params ModLoaderViewModel.ModToInstall[] modPaths)
    {
        ModLoaderViewModel? viewModel;

        try
        {
            viewModel = await ModLoaderViewModel.FromFilesAsync(gameInstallation, modPaths);
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Opening mod loader from files");

            await Services.MessageUI.DisplayExceptionMessageAsync(ex, Resources.ModLoader_ErrorOpening);

            return;    
        }
        
        if (viewModel == null)
            return;

        await ShowWindowAsync(() => new ModLoaderDialog(viewModel),
            // Only allow one Mod Loader window per installation
            typeGroupNames: new[] { viewModel.GameInstallation.InstallationId });
    }

    /// <summary>
    /// Shows a new instance of the Mod Creator
    /// </summary>
    /// <param name="gameInstallation">The game installation the mod should be created for</param>
    /// <returns>The task</returns>
    public async Task ShowModCreatorAsync(GameInstallation gameInstallation) => 
        await ShowWindowAsync(() => new ModCreatorDialog(new ModCreatorViewModel(gameInstallation)));

    /// <summary>
    /// Shows a new instance of the Prototype Restoration dialog
    /// </summary>
    /// <returns>The task</returns>
    public async Task ShowPrototypeRestorationAsync(GameInstallation gameInstallation) =>
        await ShowWindowAsync(() => new PrototypeRestorationDialog(new PrototypeRestorationViewModel(gameInstallation)), 
            typeGroupNames: new[] { gameInstallation.InstallationId });

    /// <summary>
    /// Shows a new instance of the Runtime Modifications dialog
    /// </summary>
    /// <returns>The task</returns>
    public async Task ShowRuntimeModificationsAsync(GameInstallation gameInstallation) =>
        await ShowWindowAsync(() => new RuntimeModificationsDialog(new RuntimeModificationsViewModel(gameInstallation, Services.MessageUI)), 
            typeGroupNames: new[] { gameInstallation.InstallationId });

    /// <summary>
    /// Shows a new instance of the add games dialog
    /// </summary>
    /// <returns>The task</returns>
    public async Task ShowAddGamesAsync() => 
        await ShowWindowAsync(() => new AddGamesDialog(), ShowWindowFlags.DuplicateTypesNotAllowed);

    /// <summary>
    /// Shows a new instance of the game clients setup dialog
    /// </summary>
    /// <returns>The task</returns>
    public async Task ShowGameClientsSetupAsync() => 
        await ShowWindowAsync(() => new GameClientsSetupDialog(), ShowWindowFlags.DuplicateTypesNotAllowed);

    /// <summary>
    /// Shows a new instance of the anniversary update dialog
    /// </summary>
    /// <returns>The task</returns>
    public async Task ShowAnniversaryUpdateAsync() => 
        await ShowWindowAsync(() => new AnniversaryUpdateDialog(), ShowWindowFlags.Modal);

    #endregion
}