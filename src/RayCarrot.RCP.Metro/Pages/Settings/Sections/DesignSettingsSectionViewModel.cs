using System.Windows.Input;
using System.Windows.Media;

namespace RayCarrot.RCP.Metro.Pages.Settings.Sections;

public class DesignSettingsSectionViewModel : SettingsSectionViewModel
{
    public DesignSettingsSectionViewModel(
        AppUserData data,
        AppUIManager ui) 
        : base(data)
    {
        UI = ui ?? throw new ArgumentNullException(nameof(ui));

        ChangeAccentColorCommand = new AsyncRelayCommand(ChangeAccentColorAsync);
        ResetAccentColorCommand = new RelayCommand(ResetAccentColor);
    }

    private AppUIManager UI { get; }

    public ICommand ChangeAccentColorCommand { get; }
    public ICommand ResetAccentColorCommand { get; }

    public override LocalizedString Header => new ResourceLocString(nameof(Resources.Settings_DesignHeader));
    public override GenericIconKind Icon => GenericIconKind.Settings_Design;

    public async Task ChangeAccentColorAsync()
    {
        ColorSelectionResult? result = await UI.SelectColorAsync(new ColorSelectionViewModel()
        {
            // TODO-LOC
            Title = "Select an accent color",
            SelectedColor = Data.Theme_Color ?? Colors.Black
        });

        if (result.CanceledByUser)
            return;

        Data.Theme_Color = result.SelectedColor;
    }

    public void ResetAccentColor()
    {
        Data.Theme_Color = null;
    }
}