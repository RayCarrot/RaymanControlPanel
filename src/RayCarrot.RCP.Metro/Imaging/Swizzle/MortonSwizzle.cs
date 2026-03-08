namespace RayCarrot.RCP.Metro.Imaging;

public class MortonSwizzle : Swizzle
{
    public MortonSwizzle(int width, int height, int bytesPerPixel) : base(width, height, bytesPerPixel)
    {
        Log2Width = (int)Math.Log(width, 2);
        Log2Height = (int)Math.Log(height, 2);
    }

    public int Log2Width { get; }
    public int Log2Height { get; }

    // https://github.com/Zarh/ManaGunZ
    public override int GetOffset(int x, int y)
    {
        int offset = 0;
        int t = 0;

        int log2Width = Log2Width;
        int log2Height = Log2Height;

        while (log2Width != 0 || log2Height != 0)
        {
            if (log2Width != 0)
            {
                offset |= (x & 0x01) << t;
                x >>= 1;
                ++t;
                --log2Width;
            }

            if (log2Height != 0)
            {
                offset |= (y & 0x01) << t;
                y >>= 1;
                ++t;
                --log2Height;
            }
        }

        return offset;

        // This should be more optimized, but can't get it to work
        // https://stackoverflow.com/a/30562230/9398242
        //    x &= 0x0000ffff;
        //    y &= 0x0000ffff;

        //    x = (x | (x << 8)) & 0x00FF00FF;
        //    x = (x | (x << 4)) & 0x0F0F0F0F;
        //    x = (x | (x << 2)) & 0x33333333;
        //    x = (x | (x << 1)) & 0x55555555;

        //    y = (y | (y << 8)) & 0x00FF00FF;
        //    y = (y | (y << 4)) & 0x0F0F0F0F;
        //    y = (y | (y << 2)) & 0x33333333;
        //    y = (y | (y << 1)) & 0x55555555;

        //    return x | (y << 1);
    }
}