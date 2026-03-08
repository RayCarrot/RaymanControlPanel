using System.IO;
using BinarySerializer.UbiArt;
using RayCarrot.RCP.Metro.Games.Components;

namespace RayCarrot.RCP.Metro.ModLoader.Modules.UbiArtLocalization;

public class UbiArtLocalizationModule : ModModule
{
    public const string AudioFileName = "audio.txt";

    private static string[] Locales_OriginsPC { get; } = 
    { 
        "en-US",
        "fr-FR",
        "ja-JP",
        "de-DE",
        "es-ES",
        "it-IT",
        "ko-KR",
        "zh-TW",
        "pt-PT",
        "zh-CN",
        "pl-PL",
        "ru-RU",
        "nl-NL",
        "cs-CZ",
        "hu-HU",
    };
    private static string[] Locales_LegendsPC { get; } = 
    { 
        "en-US",
        "fr-FR",
        "ja-JP",
        "de-DE",
        "es-ES",
        "it-IT",
        "ko-KR",
        "zh-TW",
        "pt-PT",
        "zh-CN",
        "pl-PL",
        "ru-RU",
        "nl-NL",
        "da-DK",
        "nb-NO",
        "sv-SE",
        "fi-FI",
        "pt-BR",
        "ms-MY",
        "id-ID",
        "tr-TR",
        "ar-SA",
        "ta-IN",
        "th-TH",
    };

    public override string Id => "ubiart-loc";
    public override LocalizedString Description => new ResourceLocString(nameof(Resources.ModLoader_UbiArtLocalizationModule_Description));

    public override void SetupModuleFolder(ModModuleViewModel viewModel, FileSystemPath modulePath)
    {
        File.Create(modulePath + "en-US.txt").Dispose();
    }

    public override IReadOnlyCollection<IFilePatch> GetPatchedFiles(Mod mod, FileSystemPath modulePath)
    {
        UbiArtSettings ubiArtSettings = mod.GameInstallation.
            GetRequiredComponent<BinaryGameModeComponent, UbiArtGameModeComponent>().
            GetSettings();

        string[] locales = ubiArtSettings switch
        {
            { Game: BinarySerializer.UbiArt.Game.RaymanOrigins, Platform: Platform.PC } => Locales_OriginsPC,
            { Game: BinarySerializer.UbiArt.Game.RaymanLegends, Platform: Platform.PC } => Locales_LegendsPC,
            _ => throw new Exception($"Unsupported game {ubiArtSettings.Game}")
        };

        List<LocaleFile> localeFiles = new();
        FileSystemPath? audioFile = null;

        // Locate the locale files
        foreach (FileSystemPath filePath in Directory.GetFiles(modulePath, "*.txt", SearchOption.TopDirectoryOnly))
        {
            string fileName = filePath.RemoveFileExtension().Name;
            int localeId = locales.FindItemIndex(x => x.Equals(fileName, StringComparison.OrdinalIgnoreCase));

            if (localeId != -1)
                localeFiles.Add(new LocaleFile(localeId, filePath));
        }

        // Locate the audio file
        if ((modulePath + AudioFileName).FileExists)
            audioFile = modulePath + AudioFileName;

        if (localeFiles.Count == 0 && audioFile == null)
            return Array.Empty<IFilePatch>();

        if (ubiArtSettings is { Game: BinarySerializer.UbiArt.Game.RaymanOrigins, Platform: Platform.PC })
        {
            ModFilePath locFilePath = new(@"localisation\localisation.loc", @"GameData\bundle_PC.ipk", UbiArtArchiveComponent.Id);
            return new UbiArtLocalizationFilePatch<String16>(mod.GameInstallation, locFilePath, localeFiles, audioFile).YieldToArray();
        }
        else if (ubiArtSettings is { Game: BinarySerializer.UbiArt.Game.RaymanLegends, Platform: Platform.PC })
        {
            ModFilePath locFilePath = new(@"EngineData\Localisation\localisation.loc8");
            return new UbiArtLocalizationFilePatch<String8>(mod.GameInstallation, locFilePath, localeFiles, audioFile).YieldToArray();
        }
        else
        {
            throw new Exception($"Unsupported game {ubiArtSettings.Game}");
        }
    }
}