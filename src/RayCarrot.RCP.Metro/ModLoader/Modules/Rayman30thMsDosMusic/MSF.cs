using BinarySerializer;

namespace RayCarrot.RCP.Metro.ModLoader.Modules.Rayman30thMsDosMusic;

public readonly struct MSF
{
    public MSF(uint value)
    {
        Minutes = (byte)BitHelpers.ExtractBits((int)value, 8, 16);
        Seconds = (byte)BitHelpers.ExtractBits((int)value, 8, 8);
        Frames = (byte)BitHelpers.ExtractBits((int)value, 8, 0);
    }

    public MSF(int minutes, int seconds, int frames)
    {
        Minutes = (byte)minutes;
        Seconds = (byte)seconds;
        Frames = (byte)frames;
    }

    private const int FramesPerSecond = 75;
    private const int SecondsPerMinute = 60;
    private const int FramesPerMinute = FramesPerSecond * SecondsPerMinute;

    public static MSF Pregap { get; } = new(0, 2, 0);

    public byte Minutes { get; }
    public byte Seconds { get; }
    public byte Frames { get; }
    public uint Value => (uint)((Minutes << 16) | (Seconds << 8) | Frames);

    public static MSF FromSeconds(double totalSeconds)
    {
        byte minutes = (byte)(totalSeconds / SecondsPerMinute);
        byte seconds = (byte)(totalSeconds % SecondsPerMinute);
        byte frames = (byte)Math.Ceiling((totalSeconds % 1) * FramesPerSecond);

        return new MSF(minutes, seconds, frames);
    }

    public static MSF FromLBA(int lba)
    {
        int minutes = lba / FramesPerMinute;
        lba %= FramesPerMinute;

        int seconds = lba / FramesPerSecond;
        lba %= FramesPerSecond;

        int frames = lba;

        return new MSF(minutes, seconds, frames);
    }

    public static MSF operator +(MSF v1, MSF v2)
    {
        int lba1 = v1.GetLBA();
        int lba2 = v2.GetLBA();

        return FromLBA(lba1 + lba2);
    }

    public static MSF operator -(MSF v1, MSF v2)
    {
        int lba1 = v1.GetLBA();
        int lba2 = v2.GetLBA();

        return FromLBA(lba1 - lba2);
    }

    public bool IsEmpty() => Minutes == 0 && Seconds == 0 && Frames == 0;
    public int GetLBA() => Seconds * FramesPerSecond + Minutes * FramesPerMinute + Frames;

    public override string ToString() => $"{Minutes:00}:{Seconds:00}:{Frames:00}";
}