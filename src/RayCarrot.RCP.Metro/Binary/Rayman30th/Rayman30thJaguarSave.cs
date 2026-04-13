#nullable disable
using BinarySerializer;
using BinarySerializer.Ray1.Jaguar;

namespace RayCarrot.RCP.Metro;

public class Rayman30thJaguarSave : BinarySerializable
{
    public JAG_SaveData SaveData { get; set; }

    public override void SerializeImpl(SerializerObject s)
    {
        // Validate the save state
        s.SerializeMagic<uint>(0x81980085);
        s.SerializeMagic<ushort>(0x32A);

        // Ignore parsing the remaining save state data. We instead make the assumption that the save data is always in the same location...
        s.Goto(Offset + 0x424B88);
        s.DoEndian(Endian.Big, () =>
        {
            SaveData = s.SerializeObject<JAG_SaveData>(SaveData, name: nameof(SaveData));
        });
    }
}