#nullable disable
using BinarySerializer;
using BinarySerializer.Audio.RIFF;

namespace RayCarrot.RCP.Metro;

public class RIFF_Chunk_Waves : RIFF_ChunkData
{
    public override string ChunkIdentifier => "wavs";

    public int WavesCount { get; set; }
    public int TablesLength { get; set; }
    public int Int_08 { get; set; }
    public int Int_0C { get; set; }

    public uint[] WaveNameHashes { get; set; } // MurmurHash3
    public int[] NameHashIndexToWaveIndexTable { get; set; }
    public Wave[] Waves { get; set; }

    public override void SerializeImpl(SerializerObject s)
    {
        WavesCount = s.Serialize<int>(WavesCount, name: nameof(WavesCount));
        TablesLength = s.Serialize<int>(TablesLength, name: nameof(TablesLength));
        Int_08 = s.Serialize<int>(Int_08, name: nameof(Int_08));
        Int_0C = s.Serialize<int>(Int_0C, name: nameof(Int_0C));
        WaveNameHashes = s.SerializeArray<uint>(WaveNameHashes, TablesLength, name: nameof(WaveNameHashes));
        NameHashIndexToWaveIndexTable = s.SerializeArray<int>(NameHashIndexToWaveIndexTable, TablesLength, name: nameof(NameHashIndexToWaveIndexTable));
        Waves = s.SerializeObjectArray<Wave>(Waves, WavesCount, name: nameof(Waves));
    }
}