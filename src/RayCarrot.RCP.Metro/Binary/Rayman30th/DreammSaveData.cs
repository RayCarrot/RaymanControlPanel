#nullable disable
using BinarySerializer;

namespace RayCarrot.RCP.Metro;

public class DreammSaveData : BinarySerializable
{
    public DreammVirtualSaveFile[] VirtualSaveFiles { get; set; }

    public override void SerializeImpl(SerializerObject s)
    {
        VirtualSaveFiles = s.SerializeObjectArrayUntil(VirtualSaveFiles, _ => s.CurrentFileOffset >= s.CurrentLength, name: nameof(VirtualSaveFiles));
    }
}