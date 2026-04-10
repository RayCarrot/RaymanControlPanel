#nullable disable
using BinarySerializer;
using BinarySerializer.Audio.RIFF;

namespace RayCarrot.RCP.Metro;

public class RIFF_Chunk_WaveData : RIFF_ChunkData
{
    public override string ChunkIdentifier => "wdta";

    public byte[] Data { get; set; }

    public override void SerializeImpl(SerializerObject s)
    {
        Data = s.SerializeArray<byte>(Data, Pre_ChunkSize, name: nameof(Data));
    }
}