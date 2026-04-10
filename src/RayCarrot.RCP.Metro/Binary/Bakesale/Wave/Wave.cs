#nullable disable
using BinarySerializer;

namespace RayCarrot.RCP.Metro;

public class Wave : BinarySerializable
{
    public uint Hash { get; set; }
    public int Index { get; set; }

    public override void SerializeImpl(SerializerObject s)
    {
        Hash = s.Serialize<uint>(Hash, name: nameof(Hash));
        Index = s.Serialize<int>(Index, name: nameof(Index));
    }
}