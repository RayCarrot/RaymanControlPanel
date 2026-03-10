namespace RayCarrot.RCP.Metro.Games.Options;

/// <summary>
/// View model for Rayman 30th Anniversary Edition game options
/// </summary>
public class Rayman30thGameOptionsViewModel : GameOptionsViewModel
{
    public Rayman30thGameOptionsViewModel(GameInstallation gameInstallation) : base(gameInstallation)
    {
        AvailableGraphicsApis =
        [
            new GraphicsApi(null, new ResourceLocString(nameof(Resources.GameOptions_GraphicsApi_Default))),
            new GraphicsApi("dx11", new ResourceLocString(nameof(Resources.GameOptions_GraphicsApi_DX11))),
            new GraphicsApi("dx12", new ResourceLocString(nameof(Resources.GameOptions_GraphicsApi_DX12))),
            new GraphicsApi("opengl", new ResourceLocString("OpenGL")) // TODO-LOC
        ];
        IsAvailable = gameInstallation.GetComponent<LaunchGameComponent>()?.SupportsLaunchArguments == true;
    }

    public ObservableCollection<GraphicsApi> AvailableGraphicsApis { get; }
    public bool IsAvailable { get; }

    public GraphicsApi SelectedGraphicsApi
    {
        get
        {
            string? id = GameInstallation.GetValue<string>(GameDataKey.R30th_GraphicsApi);
            return AvailableGraphicsApis.First(x => x.Id == id); 
        }
        set
        {
            GameInstallation.SetValue(GameDataKey.R30th_GraphicsApi, value.Id);
            Services.Messenger.Send(new ModifiedGamesMessage(GameInstallation));
        }
    }

    public class GraphicsApi : BaseViewModel
    {
        public GraphicsApi(string? id, LocalizedString displayName)
        {
            Id = id;
            DisplayName = displayName;
        }

        public string? Id { get; }
        public LocalizedString DisplayName { get; }
    }
}