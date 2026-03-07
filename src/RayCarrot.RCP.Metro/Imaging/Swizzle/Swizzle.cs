namespace RayCarrot.RCP.Metro.Imaging;

public abstract class Swizzle
{
    protected Swizzle(int width, int height, int bytesPerPixel)
    {
        Width = width;
        Height = height;
        BytesPerPixel = bytesPerPixel;
    }

    public int Width { get; }
    public int Height { get; }
    public int BytesPerPixel { get; }

    public abstract int GetOffset(int x, int y);

    public virtual byte[] Unswizzle(byte[] src)
    {
        byte[] dst = new byte[Width * Height * BytesPerPixel];

        for (int y = 0; y < Height; y++)
        {
            for (int x = 0; x < Width; x++)
            {
                int srcOffset = GetOffset(x, y) * BytesPerPixel;
                int dstOffset = (y * Width + x) * BytesPerPixel;

                Buffer.BlockCopy(src, srcOffset, dst, dstOffset, BytesPerPixel);
            }
        }

        return dst;
    }
}