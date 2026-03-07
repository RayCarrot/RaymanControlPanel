namespace RayCarrot.RCP.Metro.Imaging;

public class MortonSwizzle : Swizzle
{
    public MortonSwizzle(int width, int height, int bytesPerPixel) : base(width, height, bytesPerPixel) { }

    public override int GetOffset(int x, int y)
    {
        int morton = 0;
        for (int i = 0; i < 16; i++)
        {
            morton |= ((x >> i) & 1) << (2 * i);
            morton |= ((y >> i) & 1) << (2 * i + 1);
        }
        return morton;
    }
}