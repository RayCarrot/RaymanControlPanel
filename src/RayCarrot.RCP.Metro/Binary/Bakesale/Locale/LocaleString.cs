#nullable disable
using BinarySerializer;

namespace RayCarrot.RCP.Metro;

public class LocaleString : BinarySerializable
{
    public Pointer StringOffset { get; set; }
    public int StringLength { get; set; }

    public string Value { get; set; }

    public override void SerializeImpl(SerializerObject s)
    {
        // Serialize offsets
        StringOffset = s.SerializePointer(StringOffset, anchor: s.CurrentPointer, name: nameof(StringOffset));
        StringLength = s.Serialize<int>(StringLength, name: nameof(StringLength));

        // Serialize data from offset
        s.DoAt(StringOffset, () => Value = s.SerializeString(Value, length: StringLength, name: nameof(Value)));
    }
}