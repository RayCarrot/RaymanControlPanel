using System.Net.Http;
using Nito.AsyncEx;
using RayCarrot.RCP.Metro.ModLoader.Sources;

namespace RayCarrot.RCP.Metro.ModLoader.Dialogs.ModLoader;

public class DownloadableModsFeedViewModel : BaseViewModel, IDisposable
{
    #region Constructor

    public DownloadableModsFeedViewModel(
        ModLoaderViewModel modLoaderViewModel, 
        GameInstallation gameInstallation,
        WebImageCache webImageCache,
        HttpClient httpClient,
        DownloadableModsSource downloadableModsSource)
    {
        _asyncLock = new AsyncLock();
        _modLoaderViewModel = modLoaderViewModel;
        GameInstallation = gameInstallation;
        _webImageCache = webImageCache;
        _httpClient = httpClient;
        _downloadableModsSource = downloadableModsSource;
    }

    #endregion

    #region Logger

    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    #endregion

    #region Private Fields

    private readonly AsyncLock _asyncLock;
    private readonly ModLoaderViewModel _modLoaderViewModel;
    private readonly WebImageCache _webImageCache;
    private readonly HttpClient _httpClient;
    private readonly DownloadableModsSource _downloadableModsSource;
    private int _pageCount;
    private int _currentPage;
    private int _feedVersion; // Keep track of when we re-initialize the feed

    #endregion

    #region Public Properties

    public GameInstallation GameInstallation { get; }

    public ObservableCollectionEx<DownloadableModViewModel> Mods { get; } = new();

    public DownloadableModsFeedFilter? Filter { get; set; }

    public bool IsEmpty { get; set; }
    public string? ErrorMessage { get; set; }
    public bool CanLoadNextPage { get; set; }

    #endregion

    #region Private Methods

    private async Task LoadCurrentPageAsync()
    {
        Logger.Info("Loading page {0}", _currentPage);

        // Load the feed page
        DownloadableModsFeedPage feedPage = await _downloadableModsSource.LoadModsFeedPage(
            modLoaderViewModel: _modLoaderViewModel,
            loadedDownloadableMods: Mods,
            webImageCache: _webImageCache,
            httpClient: _httpClient,
            gameInstallation: GameInstallation,
            filter: Filter,
            page: _currentPage);

        // Get the page count
        _pageCount = feedPage.PageCount;

        // Remove the placeholder mods
        RemovePlaceholderMods();

        // Add the new mods
        Mods.AddRange(feedPage.DownloadableMods);

        Logger.Info("Loaded page {0} out of {1} total pages", _currentPage, _pageCount);
    }

    private void RemovePlaceholderMods()
    {
        Mods.ModifyCollection(mods => mods.RemoveAll(x => x is PlaceholderDownloadableModViewModel));
    }

    private void AddPlaceholderMods()
    {
        for (int i = 0; i < _downloadableModsSource.GetModsFeedPageLength(); i++)
            Mods.Add(new PlaceholderDownloadableModViewModel(_downloadableModsSource, _feedVersion, _currentPage));
    }

    #endregion

    #region Public Methods

    public async Task InitializeAsync(DownloadableModsFeedFilter? filter)
    {
        using (await _asyncLock.LockAsync())
        {
            Filter = filter;
            IsEmpty = true;
            ErrorMessage = null;
            Mods.DisposeAll();
            Mods.Clear();
            AddPlaceholderMods();
            _currentPage = -1;
            _pageCount = -1;
            _feedVersion++;
            CanLoadNextPage = true;
        }
    }

    public async Task LoadNextPageAsync()
    {
        using (await _asyncLock.LockAsync())
        {
            if (!CanLoadNextPage)
                return;

            IsEmpty = false;

            try
            {
                _currentPage++;
                await LoadCurrentPageAsync();

                CanLoadNextPage = _pageCount != -1 && _currentPage < _pageCount - 1;
                IsEmpty = !Mods.Any();

                if (!IsEmpty && CanLoadNextPage)
                    AddPlaceholderMods();

                Logger.Info("Loaded next page");
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Loading next page of downloadable mods");

                Mods.DisposeAll();
                Mods.Clear();
                ErrorMessage = ex.Message;
                IsEmpty = false;
                CanLoadNextPage = false;
            }
        }
    }

    public bool IsPlaceholderForNextPage(PlaceholderDownloadableModViewModel placeholder)
    {
        return placeholder.DownloadableModsSource.Id == _downloadableModsSource.Id && 
               placeholder.FeedVersion == _feedVersion && 
               placeholder.Page == _currentPage;
    }

    public void Dispose()
    {
        Mods.DisposeAll();
    }

    #endregion
}