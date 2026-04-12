#nullable disable
using BinarySerializer;
using BinarySerializer.Ray1;
using BinarySerializer.Ray1.PC;

namespace RayCarrot.RCP.Metro;

public class DreammVirtualSaveFile : BinarySerializable
{
    public byte VirtualFileId { get; set; }
    public SaveSlot VirtualFile { get; set; }

    public override void SerializeImpl(SerializerObject s)
    {
        VirtualFileId = s.Serialize<byte>(VirtualFileId, name: nameof(VirtualFileId));

        s.DoProcessed(new DataLengthProcessor(), p =>
        {
            p.Serialize<uint>(s, "VirtualFileSize");
            s.DoEncoded(new SaveEncoder(p.SerializedValue), () =>
            {
                VirtualFile = s.SerializeObject<SaveSlot>(VirtualFile, name: nameof(VirtualFile));
            });
        });
    }
}