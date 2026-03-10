namespace RayCarrot.RCP.Metro.Imaging;

[Flags]
public enum RawImageDataFeatures
{
    None = 0,
    Mipmaps = 1 << 0,
    BlockCompression = 1 << 1,
}