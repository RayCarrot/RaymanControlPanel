#nullable disable
using BinarySerializer;

namespace RayCarrot.RCP.Metro;

public class PancakeMemoryCardDirectory : BinarySerializable
{
    public uint Uint_00 { get; set; } // Always 0x50?
    public PancakeMemoryCardDirectoryFlags Flags { get; set; }
    public string CodeIdentifier { get; set; }
    public short Short_1D { get; set; } // Always 0?
    public byte Byte_1F { get; set; } // Always 0?

    public string CountryCode => CodeIdentifier[..2]; // BI, BA, BE
    public string ProductCode => CodeIdentifier[2..12]; // AAAA-00000
    public string Identifier => CodeIdentifier[12..]; // 8 characters

    public override void SerializeImpl(SerializerObject s)
    {
        Uint_00 = s.Serialize<uint>(Uint_00, name: nameof(Uint_00));
        Flags = s.Serialize<PancakeMemoryCardDirectoryFlags>(Flags, name: nameof(Flags));
        CodeIdentifier = s.SerializeString(CodeIdentifier, length: 21, name: nameof(CodeIdentifier));
        Short_1D = s.Serialize<short>(Short_1D, name: nameof(Short_1D));
        Byte_1F = s.Serialize<byte>(Byte_1F, name: nameof(Byte_1F));
    }
}