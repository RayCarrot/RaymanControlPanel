namespace RayCarrot.RCP.Metro.Imaging;

public class MipmapImage
{
    public MipmapImage(byte[] imageData)
    {
        ImageData = imageData;
    }

    public byte[] ImageData { get; }
}