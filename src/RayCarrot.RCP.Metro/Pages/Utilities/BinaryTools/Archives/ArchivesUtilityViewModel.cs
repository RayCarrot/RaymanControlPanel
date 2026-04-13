using System.Windows.Input;
using RayCarrot.RCP.Metro.Archive;
using RayCarrot.RCP.Metro.Archive.Bakesale;
using RayCarrot.RCP.Metro.Archive.CPA;
using RayCarrot.RCP.Metro.Archive.Ray1;
using RayCarrot.RCP.Metro.Archive.UbiArt;

namespace RayCarrot.RCP.Metro.Pages.Utilities;

public class ArchivesUtilityViewModel : UtilityViewModel
{
    #region Constructor

    public ArchivesUtilityViewModel()
    {
        Types = new ObservableCollection<ArchivesUtilityTypeViewModel>()
        {
            new ArchivesUtilityTypeViewModel(
                name: new ResourceLocString(resourcekey: nameof(Resources.Utilities_ArchiveExplorer_R1Header)),
                fileExtension: new FileExtension(".dat"),
                getManagerFunc: (data, mode) => new Ray1PCArchiveDataManager(data.GetAttribute<Ray1GameModeInfoAttribute>().GetSettings()),
                modes: new EnumSelectionViewModel<Enum>(Ray1GameMode.RaymanDesigner_PC, new Enum[]
                {
                    Ray1GameMode.RaymanEducational_PC,
                    Ray1GameMode.RaymanDesigner_PC,
                    Ray1GameMode.RaymanByHisFans_PC,
                    Ray1GameMode.Rayman60Levels_PC,
                })),

            new ArchivesUtilityTypeViewModel(
                name: new ResourceLocString(resourcekey: nameof(Resources.Utilities_ArchiveExplorer_CNTHeader)),
                fileExtension: new FileExtension(".cnt"),
                getManagerFunc: (data, mode) => new CPACntArchiveDataManager(data.GetAttribute<CPAGameModeInfoAttribute>().GetSettings(), null, null),
                modes: new EnumSelectionViewModel<Enum>(CPAGameMode.Rayman2_PC, new Enum[]
                {
                    CPAGameMode.Rayman2_PC,
                    CPAGameMode.Rayman2_Demo1_PC,
                    CPAGameMode.Rayman2_Demo2_PC,
                    CPAGameMode.RaymanM_PC,
                    CPAGameMode.RaymanArena_PC,
                    CPAGameMode.Rayman3_PC,
                    CPAGameMode.TonicTrouble_PC,
                    CPAGameMode.TonicTrouble_SE_PC,
                    CPAGameMode.DonaldDuck_PC,
                    CPAGameMode.PlaymobilHype_PC,
                })),

            new ArchivesUtilityTypeViewModel(
                name: new ResourceLocString(resourcekey: nameof(Resources.Utilities_ArchiveExplorer_IPKHeader)),
                fileExtension: new FileExtension(".ipk"),
                getManagerFunc: (data, mode) => new UbiArtIPKArchiveDataManager(
                    settings: data.GetAttribute<UbiArtGameModeInfoAttribute>().GetSettings(), 
                    gameInstallation: null, 
                    compressionMode: mode == ArchivesUtilityTypeViewModel.ArchiveMode.Explorer 
                    ? UbiArtIPKArchiveConfigViewModel.FileCompressionMode.WasCompressed 
                    : UbiArtIPKArchiveConfigViewModel.FileCompressionMode.MatchesSetting),
                modes: new EnumSelectionViewModel<Enum>(UbiArtGameMode.RaymanOrigins_PC, new Enum[]
                {
                    UbiArtGameMode.RaymanOrigins_PC,
                    UbiArtGameMode.RaymanOrigins_PS3,
                    UbiArtGameMode.RaymanOrigins_Xbox360,
                    UbiArtGameMode.RaymanOrigins_Wii,
                    UbiArtGameMode.RaymanOrigins_PSVita,
                    UbiArtGameMode.RaymanOrigins_3DS,
                    UbiArtGameMode.RaymanLegends_PC,
                    UbiArtGameMode.RaymanLegends_PS3,
                    UbiArtGameMode.RaymanLegends_Xbox360,
                    UbiArtGameMode.RaymanLegends_WiiU,
                    UbiArtGameMode.RaymanLegends_PSVita,
                    UbiArtGameMode.RaymanLegends_PS4,
                    UbiArtGameMode.RaymanLegends_XboxOne,
                    UbiArtGameMode.RaymanLegends_Switch,
                    UbiArtGameMode.RaymanAdventures_Android,
                    UbiArtGameMode.RaymanAdventures_iOS,
                    UbiArtGameMode.RaymanMini_Mac,
                    UbiArtGameMode.ChildOfLight_PC,
                    UbiArtGameMode.ChildOfLight_PSVita,
                    UbiArtGameMode.ValiantHearts_Android,
                    UbiArtGameMode.GravityFalls_3DS,
                    UbiArtGameMode.JustDance_2014_WiiU,
                    UbiArtGameMode.JustDance_2014_WiiU_JP,
                    UbiArtGameMode.JustDance_YoKaiWatch_WiiU,
                    UbiArtGameMode.JustDance_2015_WiiU,
                    UbiArtGameMode.JustDance_2016_PS3,
                    UbiArtGameMode.JustDance_2016_WiiU,
                    UbiArtGameMode.JustDance_2016_WiiU_Demo,
                    UbiArtGameMode.JustDance_2017_PC,
                    UbiArtGameMode.JustDance_2017_WiiU,
                    UbiArtGameMode.JustDance_2017_WiiU_Demo,
                    UbiArtGameMode.JustDance_2018_WiiU,
                    UbiArtGameMode.JustDance_2018_WiiU_Demo,
                    UbiArtGameMode.JustDance_2019_WiiU,
                    UbiArtGameMode.JustDance_2019_WiiU_Demo,
                    UbiArtGameMode.JustDance_2020_Wii,
                    UbiArtGameMode.JustDance_2020_Switch,
                    UbiArtGameMode.JustDance_2021_Switch,
                    UbiArtGameMode.JustDance_2022_Switch,
                })),

            new ArchivesUtilityTypeViewModel(
                name: new ResourceLocString(nameof(Resources.Utilities_ArchiveExplorer_PIEHeader)),
                fileExtension: new FileExtension(".pie"),
                getManagerFunc: (data, mode) => new BakesalePieArchiveDataManager(
                    gameKey: data.GetRequiredAttribute<BakesaleGameModeInfoAttribute>().GameKey),
                modes: new EnumSelectionViewModel<Enum>(BakesaleGameMode.Rayman30thAnniversaryEdition_PC, new Enum[]
                {
                    BakesaleGameMode.Rayman30thAnniversaryEdition_PC,
                })),
        };
        SelectedType = Types.First();

        OpenArchiveCommand = new AsyncRelayCommand(OpenArchiveExplorerAsync);
        CreateArchiveCommand = new AsyncRelayCommand(CreateArchiveAsync);
    }

    #endregion

    #region Logger

    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    #endregion

    #region Commands

    public ICommand OpenArchiveCommand { get; }
    public ICommand CreateArchiveCommand { get; }

    #endregion

    #region Public Properties

    public override LocalizedString DisplayHeader => new ResourceLocString(nameof(Resources.Utilities_ArchiveExplorer_Header));
    public override GenericIconKind Icon => GenericIconKind.Utilities_ArchiveExplorer;

    public ObservableCollection<ArchivesUtilityTypeViewModel> Types { get; }
    public ArchivesUtilityTypeViewModel SelectedType { get; set; }

    #endregion

    #region Public Methods

    public async Task OpenArchiveExplorerAsync()
    {
        GameInstallation? gameInstallation = GameModeHelpers.FindGameInstallation(Services.Games, SelectedType.Modes.SelectedValue);

        // Allow the user to select the files
        FileBrowserResult fileResult = await Services.BrowseUI.BrowseFileAsync(new FileBrowserViewModel()
        {
            Title = Resources.Utilities_ArchiveExplorer_FileSelectionHeader,
            DefaultDirectory = gameInstallation?.InstallLocation.Directory ?? FileSystemPath.EmptyPath,
            ExtensionFilter = SelectedType.FileExtension.GetFileFilterItem.ToString(),
            MultiSelection = true,
        });

        if (fileResult.CanceledByUser)
            return;

        // Get the manager
        using IArchiveDataManager manager = SelectedType.GetManager(ArchivesUtilityTypeViewModel.ArchiveMode.Explorer);

        try
        {
            // Show the Archive Explorer
            await Services.UI.ShowArchiveExplorerAsync(manager, fileResult.SelectedFiles);
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Archive explorer");

            await Services.MessageUI.DisplayExceptionMessageAsync(ex, Resources.Archive_CriticalError);
        }
    }

    public async Task CreateArchiveAsync()
    {
        // Get the manager
        using IArchiveDataManager manager = SelectedType.GetManager(ArchivesUtilityTypeViewModel.ArchiveMode.Creator);

        // Show the Archive Creator
        await Services.UI.ShowArchiveCreatorAsync(manager);
    }

    public override void Dispose()
    {
        base.Dispose();
        Types.DisposeAll();
    }

    #endregion
}