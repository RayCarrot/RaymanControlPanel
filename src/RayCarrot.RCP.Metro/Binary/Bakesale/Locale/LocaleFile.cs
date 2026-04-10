#nullable disable
using BinarySerializer;

namespace RayCarrot.RCP.Metro;

public class LocaleFile : BinarySerializable
{
    public Pointer StringKeyHashesOffset { get; set; }
    public int StringKeyHashesCount { get; set; }
    public Pointer KeyHashIndexToStringIndexTableOffset { get; set; }
    public int KeyHashIndexToStringIndexTableCount { get; set; }
    public Pointer LanguagesOffset { get; set; }
    public int LanguagesCount { get; set; }

    public uint[] StringKeyHashes { get; set; } // MurmurHash3
    public int[] KeyHashIndexToStringIndexTable { get; set; }
    public LocaleLanguage[] Languages { get; set; }

    public override void SerializeImpl(SerializerObject s)
    {
        // Serialize offsets
        StringKeyHashesOffset = s.SerializePointer(StringKeyHashesOffset, anchor: s.CurrentPointer, name: nameof(StringKeyHashesOffset));
        StringKeyHashesCount = s.Serialize<int>(StringKeyHashesCount, name: nameof(StringKeyHashesCount));
        KeyHashIndexToStringIndexTableOffset = s.SerializePointer(KeyHashIndexToStringIndexTableOffset, anchor: s.CurrentPointer, name: nameof(KeyHashIndexToStringIndexTableOffset));
        KeyHashIndexToStringIndexTableCount = s.Serialize<int>(KeyHashIndexToStringIndexTableCount, name: nameof(KeyHashIndexToStringIndexTableCount));
        LanguagesOffset = s.SerializePointer(LanguagesOffset, anchor: s.CurrentPointer, name: nameof(LanguagesOffset));
        LanguagesCount = s.Serialize<int>(LanguagesCount, name: nameof(LanguagesCount));

        // Serialize data from offset
        s.DoAt(StringKeyHashesOffset, () => StringKeyHashes = s.SerializeArray<uint>(StringKeyHashes, StringKeyHashesCount, name: nameof(StringKeyHashes)));
        s.DoAt(KeyHashIndexToStringIndexTableOffset, () => KeyHashIndexToStringIndexTable = s.SerializeArray<int>(KeyHashIndexToStringIndexTable, KeyHashIndexToStringIndexTableCount, name: nameof(KeyHashIndexToStringIndexTable)));
        s.DoAt(LanguagesOffset, () => Languages = s.SerializeObjectArray<LocaleLanguage>(Languages, LanguagesCount, name: nameof(Languages)));
    }
}