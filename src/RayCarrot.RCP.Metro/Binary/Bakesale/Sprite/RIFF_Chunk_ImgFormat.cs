#nullable disable
using BinarySerializer;
using BinarySerializer.Audio.RIFF;

namespace RayCarrot.RCP.Metro;

public class RIFF_Chunk_ImgFormat : RIFF_ChunkData
{
    public override string ChunkIdentifier => "fmt ";

    public string Name { get; set; }
    public string Path { get; set; }
    public uint Unknown { get; set; }
    public string FormatType { get; set; }
    public ushort Width { get; set; }
    public ushort Height { get; set; }

    public override void SerializeImpl(SerializerObject s)
    {
        Name = s.SerializeString(Name, length: 10, name: nameof(Name));
        Path = s.SerializeString(Path, length: 54, name: nameof(Path));
        Unknown = s.Serialize<uint>(Unknown, name: nameof(Unknown));
        FormatType = s.SerializeString(FormatType, length: 4, name: nameof(FormatType));
        Width = s.Serialize<ushort>(Width, name: nameof(Width));
        Height = s.Serialize<ushort>(Height, name: nameof(Height));
    }
}