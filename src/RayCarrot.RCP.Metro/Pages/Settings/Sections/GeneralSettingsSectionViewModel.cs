using System.Windows.Input;

namespace RayCarrot.RCP.Metro.Pages.Settings.Sections;

public class GeneralSettingsSectionViewModel : SettingsSectionViewModel
{
    public GeneralSettingsSectionViewModel(
        AppUserData data, 
        AppUIManager ui, 
        JumpListManager jumpListManager, 
        DiscordManager discordManager,
        RunningGamesManager runningGamesManager) 
        : base(data)
    {
        UI = ui ?? throw new ArgumentNullException(nameof(ui));
        JumpListManager = jumpListManager ?? throw new ArgumentNullException(nameof(jumpListManager));
        DiscordManager = discordManager ?? throw new ArgumentNullException(nameof(discordManager));
        RunningGamesManager = runningGamesManager ?? throw new ArgumentNullException(nameof(runningGamesManager));

        EditJumpListCommand = new AsyncRelayCommand(EditJumpListAsync);
    }

    private AppUIManager UI { get; }
    private JumpListManager JumpListManager { get; }
    private DiscordManager DiscordManager { get; }
    private RunningGamesManager RunningGamesManager { get; }

    public ICommand EditJumpListCommand { get; }

    public override LocalizedString Header => new ResourceLocString(nameof(Resources.Settings_GeneralHeader));
    public override GenericIconKind Icon => GenericIconKind.Settings_General;

    public bool CheckForRunningGames
    {
        get => Data.App_CheckForRunningGames;
        set
        {
            Data.App_CheckForRunningGames = value;

            if (value)
                RunningGamesManager.Start();
            else
                RunningGamesManager.Stop();
        }
    }

    public bool UseDiscordRichPresence
    {
        get => Data.App_UseDiscordRichPresence;
        set
        {
            Data.App_UseDiscordRichPresence = value;

            if (value)
                DiscordManager.Initialize();
            else
                DiscordManager.Uninitialize();
        }
    }

    /// <summary>
    /// Edits the jump list items
    /// </summary>
    /// <returns>The task</returns>
    public async Task EditJumpListAsync()
    {
        // Get the result
        JumpListEditResult result = await UI.EditJumpListAsync(new JumpListEditViewModel());

        if (result.CanceledByUser)
            return;

        // Update the jump list items collection
        Data.App_AutoSortJumpList = result.AutoSort;
        JumpListManager.SetItems(result.IncludedItems.Select(x => new JumpListItem(x.GameInstallation.InstallationId, x.Id)));
    }
}