#nullable disable
using BinarySerializer;

namespace RayCarrot.RCP.Metro;

public class Sprite : BinarySerializable
{
    public int XPosition { get; set; }
    public int YPosition { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
    public int Int_10 { get; set; }
    public int Int_14 { get; set; }
    public int Int_18 { get; set; }
    public int Int_1C { get; set; }
    public int ImageWidth { get; set; } // Always 0 in file
    public int ImageHeight { get; set; } // Always 0 in file
    public ushort ImageIndex { get; set; }
    public ushort Ushort_2A { get; set; } // Always 0 in file
    public int Int_2C { get; set; } // Always 0 in file
    public long ImagePixelOffset { get; set; } // Always 0 in file

    public override void SerializeImpl(SerializerObject s)
    {
        XPosition = s.Serialize<int>(XPosition, name: nameof(XPosition));
        YPosition = s.Serialize<int>(YPosition, name: nameof(YPosition));
        Width = s.Serialize<int>(Width, name: nameof(Width));
        Height = s.Serialize<int>(Height, name: nameof(Height));
        Int_10 = s.Serialize<int>(Int_10, name: nameof(Int_10));
        Int_14 = s.Serialize<int>(Int_14, name: nameof(Int_14));
        Int_18 = s.Serialize<int>(Int_18, name: nameof(Int_18));
        Int_1C = s.Serialize<int>(Int_1C, name: nameof(Int_1C));
        ImageWidth = s.Serialize<int>(ImageWidth, name: nameof(ImageWidth));
        ImageHeight = s.Serialize<int>(ImageHeight, name: nameof(ImageHeight));
        ImageIndex = s.Serialize<ushort>(ImageIndex, name: nameof(ImageIndex));
        Ushort_2A = s.Serialize<ushort>(Ushort_2A, name: nameof(Ushort_2A));
        Int_2C = s.Serialize<int>(Int_2C, name: nameof(Int_2C));
        ImagePixelOffset = s.Serialize<long>(ImagePixelOffset, name: nameof(ImagePixelOffset));
    }
}