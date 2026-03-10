using System.Numerics;

namespace RayCarrot.RCP.Metro.Imaging;

// TODO: This might have issues for smaller res textures.
//       Maybe just use this implementation: https://github.com/KillzXGaming/Switch-Toolbox/blob/master/Switch_Toolbox_Library/Texture%20Decoding/Switch/TegraX1Swizzle.cs
public class TegraX1Swizzle : Swizzle
{
    public TegraX1Swizzle(int width, int height, int bytesPerPixel) : base(width, height, bytesPerPixel)
    {
        XB = BitOperations.TrailingZeroCount(Pow2RoundUp(Width));
        YB = BitOperations.TrailingZeroCount(Pow2RoundUp(Height));

        int hh = Pow2RoundUp(Height) >> 1;

        if (!IsPow2(Height) && Height <= hh + hh / 3 && YB > 3)
            YB -= 1;

        Width2 = RoundSize(Width, BytesPerPixel switch
        {
            1 => 64,
            2 => 32,
            4 => 16,
            8 => 8,
            16 => 4,
            _ => throw new ArgumentOutOfRangeException(nameof(BytesPerPixel), BytesPerPixel, null)
        });

        XBase = BytesPerPixel switch
        {
            1 => 4,
            2 => 3,
            4 => 2,
            8 => 1,
            16 => 0,
            _ => throw new ArgumentOutOfRangeException(nameof(BytesPerPixel), BytesPerPixel, null)
        };
    }

    public int XBase { get; }
    public int XB { get; }
    public int YB { get; }
    public int Width2 { get; }

    private static int Pow2RoundUp(int v)
    {
        v -= 1;

        v |= (v + 1) >> 1;
        v |= v >> 2;
        v |= v >> 4;
        v |= v >> 8;
        v |= v >> 16;

        return v + 1;
    }

    private static bool IsPow2(int v)
    {
        return v != 0 && (v & (v - 1)) == 0;
    }

    private static int RoundSize(int size, int pad)
    {
        int mask = pad - 1;
        if ((size & mask) != 0)
        {
            size &= ~mask;
            size += pad;
        }

        return size;
    }

    public override int GetOffset(int x, int y)
    {
        int xCnt = XBase;
        int yCnt = 1;
        int xUsed = 0;
        int yUsed = 0;
        int offset = 0;

        while (xUsed < XBase + 2 && xUsed + xCnt < XB)
        {
            int xMask = (1 << xCnt) - 1;
            int yMask = (1 << yCnt) - 1;

            offset |= (x & xMask) << (xUsed + yUsed);
            offset |= (y & yMask) << (xUsed + yUsed + xCnt);

            x >>= xCnt;
            y >>= yCnt;

            xUsed += xCnt;
            yUsed += yCnt;

            xCnt = Math.Max(Math.Min(XB - xUsed, 1), 0);
            yCnt = Math.Max(Math.Min(YB - yUsed, yCnt << 1), 0);
        }

        offset |= (x + y * (Width2 >> xUsed)) << (xUsed + yUsed);

        return offset;
    }
}