#nullable disable
using BinarySerializer;
using BinarySerializer.PlayStation.PS1.MemoryCard;

namespace RayCarrot.RCP.Metro;

public class PancakeMemoryCard<T> : BinarySerializable
    where T : BinarySerializable, new()
{
    private const int DirectoriesCount = 8;

    public int Int_10 { get; set; }
    public int Int_14 { get; set; }
    public long Long_18 { get; set; } // Has to be 8
    public long Long_20 { get; set; } // Has to be 8
    public PancakeMemoryCardDirectory[] Directories { get; set; }
    public uint[] DirectoryFlags { get; set; } // 1 bit for if it's valid and then remaining bits for block index?
    public DataBlock<T>[] DataBlocks { get; set; }

    public override void SerializeImpl(SerializerObject s)
    {
        s.SerializeMagic<ulong>(0x78e90e93c48eee82);
        s.SerializeMagic<ulong>(0xcaf1c48ed00fe49a);
        Int_10 = s.Serialize<int>(Int_10, name: nameof(Int_10));
        Int_14 = s.Serialize<int>(Int_14, name: nameof(Int_14));
        Long_18 = s.Serialize<long>(Long_18, name: nameof(Long_18));
        Long_20 = s.Serialize<long>(Long_20, name: nameof(Long_20));
        Directories = s.SerializeObjectArray<PancakeMemoryCardDirectory>(Directories, DirectoriesCount, name: nameof(Directories));
        DirectoryFlags = s.SerializeArray<uint>(DirectoryFlags, DirectoriesCount, name: nameof(DirectoryFlags));

        DataBlocks = s.InitializeArray(DataBlocks, DirectoriesCount);
        s.DoArray(DataBlocks, (obj, i, name) =>
        {
            if ((Directories[i].Flags & (PancakeMemoryCardDirectoryFlags.Created | PancakeMemoryCardDirectoryFlags.InUse)) == (PancakeMemoryCardDirectoryFlags.Created | PancakeMemoryCardDirectoryFlags.InUse))
                obj = s.SerializeObject<DataBlock<T>>(obj, name: name);
            s.SerializePadding(0x2000 - (obj?.SerializedSize ?? 0));
            return obj;
        }, name: nameof(DataBlocks));
    }
}