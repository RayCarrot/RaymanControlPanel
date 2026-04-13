using System.Windows.Media.Imaging;

namespace RayCarrot.RCP.Metro;

public static class BitmapImageHelpers
{
    public static BitmapImage CreateFromFile(string filePath)
    {
        BitmapImage img = new();
        img.BeginInit();
        img.CreateOptions |= BitmapCreateOptions.IgnoreImageCache;
        img.CacheOption = BitmapCacheOption.OnLoad; // Required to allow the file to be deleted, such as if a temp file
        img.UriSource = new Uri(filePath);
        img.EndInit();

        if (img.CanFreeze)
            img.Freeze();

        return img;
    }
}