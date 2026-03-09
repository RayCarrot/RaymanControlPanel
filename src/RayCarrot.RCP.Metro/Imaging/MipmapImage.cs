namespace RayCarrot.RCP.Metro.Imaging;

public class MipmapImage
{
    public MipmapImage(byte[] imageData, int width, int height)
    {
        ImageData = imageData;
        Width = width;
        Height = height;
    }

    public int Width { get; }
    public int Height { get; }
    public byte[] ImageData { get; }
}