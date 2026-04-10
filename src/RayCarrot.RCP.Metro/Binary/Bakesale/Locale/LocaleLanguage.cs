#nullable disable
using BinarySerializer;

namespace RayCarrot.RCP.Metro;

public class LocaleLanguage : BinarySerializable
{
    public Pointer LanguageCodeOffset { get; set; }
    public int LanguageCodeLength { get; set; }
    public Pointer StringsOffset { get; set; }
    public int StringsCount { get; set; }

    public string LanguageCode { get; set; }
    public LocaleString[] Strings { get; set; }

    public override void SerializeImpl(SerializerObject s)
    {
        // Serialize offsets
        LanguageCodeOffset = s.SerializePointer(LanguageCodeOffset, anchor: s.CurrentPointer, name: nameof(LanguageCodeOffset));
        LanguageCodeLength = s.Serialize<int>(LanguageCodeLength, name: nameof(LanguageCodeLength));
        StringsOffset = s.SerializePointer(StringsOffset, anchor: s.CurrentPointer, name: nameof(StringsOffset));
        StringsCount = s.Serialize<int>(StringsCount, name: nameof(StringsCount));

        // Serialize data from offset
        s.DoAt(LanguageCodeOffset, () => LanguageCode = s.SerializeString(LanguageCode, length: LanguageCodeLength, name: nameof(LanguageCode)));
        s.DoAt(StringsOffset, () => Strings = s.SerializeObjectArray<LocaleString>(Strings, StringsCount, name: nameof(Strings)));
    }
}