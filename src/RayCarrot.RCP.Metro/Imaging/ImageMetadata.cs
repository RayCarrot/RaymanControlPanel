namespace RayCarrot.RCP.Metro.Imaging;

public readonly struct ImageMetadata
{
    public ImageMetadata(int width, int height)
    {
        Width = width;
        Height = height;
    }

    public int Width { get; }
    public int Height { get; }
}