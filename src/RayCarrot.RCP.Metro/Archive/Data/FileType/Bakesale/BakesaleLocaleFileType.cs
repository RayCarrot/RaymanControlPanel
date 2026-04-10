using System.IO;
using System.IO.Compression;
using BinarySerializer;
using MahApps.Metro.IconPacks;

namespace RayCarrot.RCP.Metro.Archive.Bakesale;

public sealed partial class BakesaleLocaleFileType : FileType
{
    #region Constructor

    public BakesaleLocaleFileType()
    {
        SubFileType = new SubFileType(
            importFormats: new FileExtension[]
            {
                // TODO: Allow importing
            },
            exportFormats: new FileExtension[]
            {
                new(".zip")
            });
    }

    #endregion

    #region Private Properties

    private SubFileType SubFileType { get; }
    private StringCache? StringCache { get; set; }

    #endregion

    #region Public Properties

    public override string TypeDisplayName => "String Locales"; // TODO-LOC
    public override PackIconMaterialKind Icon => PackIconMaterialKind.FileDocumentOutline;

    #endregion

    #region Interface Implementations

    public override bool IsSupported(IArchiveDataManager manager) => manager is BakesalePieArchiveDataManager;

    public override bool IsOfType(FileExtension fileExtension, IArchiveDataManager manager) => fileExtension.PrimaryFileExtension == ".strings";

    public override SubFileType GetSubType(FileExtension fileExtension, ArchiveFileStream inputStream, IArchiveDataManager manager) => SubFileType;

    public override void ConvertTo(
        FileExtension inputFormat, 
        FileExtension outputFormat, 
        ArchiveFileStream inputStream, 
        Stream outputStream, 
        IArchiveDataManager manager)
    {
        // Create the string cache if needed
        if (StringCache == null)
        {
            StringCache = new StringCache();
            StringCache.Add(Strings);
        }

        // Create a zip file to write the sounds to
        using ZipArchive zip = new(outputStream, ZipArchiveMode.Create, leaveOpen: true);

        // Create the context
        using Context context = new RCPContext(String.Empty);

        // Read the resource file
        LocaleFile locale = context.ReadStreamData<LocaleFile>(inputStream.Stream, name: inputStream.Name, mode: VirtualFileMode.DoNotClose, allowLocalPointers: true);

        // Get the string key hashes
        Dictionary<int, uint> stringHashes = new();
        for (int i = 0; i < locale.StringKeyHashes.Length; i++)
        {
            uint hash = locale.StringKeyHashes[i];
            if (hash != 0)
                stringHashes.Add(locale.KeyHashIndexToStringIndexTable[i], hash);
        }

        // Export each language
        foreach (LocaleLanguage language in locale.Languages)
        {
            // Get the strings and their keys
            SortedDictionary<string, string> strings = new();
            for (int i = 0; i < language.Strings.Length; i++)
            {
                uint hash = stringHashes[i];
                if (!StringCache.TryGetValue(hash, out string? key))
                    key = $"_unnamed_{hash:X8}";
                strings[key] = language.Strings[i].Value;
            }

            // Export the file
            ZipArchiveEntry zipEntry = zip.CreateEntry($"{language.LanguageCode}.json", CompressionLevel.Fastest);
            using Stream zipEntryStream = zipEntry.Open();
            JsonHelpers.SerializeToStream(strings, zipEntryStream);
        }
    }

    public override void ConvertFrom(
        FileExtension inputFormat, 
        FileExtension outputFormat, 
        ArchiveFileStream currentFileStream, 
        ArchiveFileStream inputStream, 
        ArchiveFileStream outputStream, 
        IArchiveDataManager manager)
    {
        throw new NotSupportedException("Importing .strings files is not supported");
    }

    #endregion
}