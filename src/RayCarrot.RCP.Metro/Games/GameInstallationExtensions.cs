using System.IO;

namespace RayCarrot.RCP.Metro;

public static class GameInstallationExtensions
{
    /// <summary>
    /// Gets the localized or custom display name for the game installation
    /// </summary>
    /// <param name="gameInstallation">The game installation to get the display name for</param>
    /// <returns>The display name</returns>
    public static LocalizedString GetDisplayName(this GameInstallation gameInstallation)
    {
        string? customName = gameInstallation.GetValue<string>(GameDataKey.RCP_CustomName);
        return customName != null
            ? new ConstLocString(customName)
            : gameInstallation.GameDescriptor.DisplayName;
    }

    public static object GetIconAssetSource(this GameInstallation gameInstallation)
    {
        // Get the custom icon image
        if (gameInstallation.GetValue<string?>(GameDataKey.RCP_IconImage) is { } iconImage)
        {
            // Return if it exists
            if (File.Exists(iconImage))
                return BitmapImageHelpers.CreateFromFile(iconImage);

            // Remove if it does not exist
            gameInstallation.SetValue<string?>(GameDataKey.RCP_IconImage, null);
            Services.Messenger.Send(new ModifiedGameIconMessage(gameInstallation));
        }

        // Default to the default icon asset
        return gameInstallation.GameDescriptor.Icon.GetAssetPath();
    }

    public static object GetBannerAssetSource(this GameInstallation gameInstallation)
    {
        // Get the custom banner image
        if (gameInstallation.GetValue<string?>(GameDataKey.RCP_BannerImage) is { } bannerImage)
        {
            // Return if it exists
            if (File.Exists(bannerImage))
                return BitmapImageHelpers.CreateFromFile(bannerImage);

            // Remove if it does not exist
            gameInstallation.SetValue<string?>(GameDataKey.RCP_BannerImage, null);
        }

        // Default to the default banner asset
        return gameInstallation.GameDescriptor.Banner.GetAssetPath();
    }
}