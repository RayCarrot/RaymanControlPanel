namespace RayCarrot.RCP.Metro;

// TODO: Refactor this, maybe split into multiple classes.

/// <summary>
/// Commons URLs used in the Rayman Control Panel
/// </summary>
public static class AppURLs
{
    #region GitHub

    public const string GitHubUserName = "RayCarrot";
    public const string GitHubRepoName = "RaymanControlPanel";

    public const string GitHubReleaseExeFileName = "RaymanControlPanel.exe";
    public const string GitHubReleaseAlternateExeFileName = "Updater.exe";
    public const string GitHubReleaseChangelogFileName = "Changelog.txt";

    public const string GitHubHostedNewsFilePath = "hosted/news.json";
    public const string GitHubHostedFeaturedGameBananaModsFilePath = "hosted/featured_gb_mods.jsonc";

    /// <summary>
    /// The GitHub repository URL
    /// </summary>
    public const string GitHubRepoUrl = $"https://github.com/{GitHubUserName}/{GitHubRepoName}";

    /// <summary>
    /// The Mod Loader wiki documentation URL
    /// </summary>
    public const string ModLoaderDocumentationUrl = $"{GitHubRepoUrl}/wiki/Mod-Loader";

    /// <summary>
    /// The latest GitHub release URL
    /// </summary>
    public const string LatestGitHubReleaseUrl = $"{GitHubRepoUrl}/releases/latest";

    #endregion

    #region GameBanana

    public const string RaymanForeverCompleteSoundtrackGameBananaFileId = "1635058"; // https://gamebanana.com/sounds/86562
    public const string RaymanDesignerCleanFilesGameBananaFileId = "1635146"; // https://gamebanana.com/mods/656210
    public const string RaymanRavingRabbidsGOGPatchedBFGameBananaFileId = "1635188"; // https://gamebanana.com/mods/656226
    public const string RaymanRavingRabbidsSteamPatchedBFGameBananaFileId = "1635199"; // https://gamebanana.com/mods/656226

    #endregion

    #region Contact

    /// <summary>
    /// The YouTube URL
    /// </summary>
    public const string YouTubeUrl = "https://www.youtube.com/c/RayCarrot";

    /// <summary>
    /// The Bluesky URL
    /// </summary>
    public const string BlueskyUrl = "https://bsky.app/profile/raycarrot.bsky.social";

    /// <summary>
    /// The Steam group URL
    /// </summary>
    public const string SteamUrl = "https://steamcommunity.com/groups/RaymanControlPanel";

    /// <summary>
    /// The translation contribution URL
    /// </summary>
    public const string TranslationUrl = "https://steamcommunity.com/groups/RaymanControlPanel/discussions/0/1812044473314212117/";

    #endregion

    #region raym.app

    /// <summary>
    /// The base URL for RayMap
    /// </summary>
    public const string RayMapBaseUrl = "https://raym.app/maps/";

    /// <summary>
    /// The base URL for Ray1Map
    /// </summary>
    public const string Ray1MapBaseUrl = "https://raym.app/maps_r1/";

    /// <summary>
    /// Gets the game URL for RayMap
    /// </summary>
    /// <param name="mode">The game mode</param>
    /// <param name="folder">The game folder, as a relative path</param>
    /// <returns>The URL</returns>
    public static string GetRayMapGameURL(string mode, string folder) => $"{RayMapBaseUrl}?mode={mode}&folder={folder}";

    /// <summary>
    /// Gets the game URL for Ray1Map
    /// </summary>
    /// <param name="mode">The game mode</param>
    /// <param name="folder">The game folder, as a relative path</param>
    /// <param name="vol">Optional volume argument</param>
    /// <returns>The URL</returns>
    public static string GetRay1MapGameURL(string mode, string folder, string? vol = null) => $"{Ray1MapBaseUrl}?mode={mode}&folder={folder}{(vol != null ? $"&vol={vol}" : String.Empty)}";

    #endregion
}