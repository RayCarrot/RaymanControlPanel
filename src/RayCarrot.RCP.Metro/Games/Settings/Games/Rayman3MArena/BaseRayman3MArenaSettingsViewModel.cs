using System.IO;
using BinarySerializer;
using BinarySerializer.OpenSpace;
using RayCarrot.RCP.Metro.Games.Structure;
using RayCarrot.RCP.Metro.Ini;

namespace RayCarrot.RCP.Metro.Games.Settings;

/// <summary>
/// Base settings view model for Rayman M, Rayman Arena and Rayman 3
/// </summary>
public abstract class BaseRayman3MArenaSettingsViewModel<TAppData, TLanguage> : GameSettingsViewModel
    where TAppData : IniAppData, new()
    where TLanguage : struct, Enum
{
    #region Constructor

    protected BaseRayman3MArenaSettingsViewModel(GameInstallation gameInstallation) : base(gameInstallation)
    {
        AppDataManager = new UbisoftIniAppDataManager<TAppData>(AppFilePaths.UbiIniPath);

        GraphicsMode = new GraphicsModeSelectionViewModel();
        GraphicsMode.GraphicsModeChanged += (_, _) => UnsavedChanges = true;

        CanModifyGameFiles = Services.File.CheckDirectoryWriteAccess(GameInstallation.InstallLocation.Directory);

        if (!CanModifyGameFiles)
            Logger.Info("The game files {0} can't be modified", GameInstallation.FullId);

        ModemQualityOptions = new[]
        {
            "Unknown",
            "Modem 56k",
            "RNIS",
            "xDSL or cable",
            "Local Area Network"
        };
    }

    #endregion

    #region Logger

    // ReSharper disable once StaticMemberInGenericType
    private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

    #endregion

    #region Protected Abstract Properties

    protected FilePatcher? Patcher { get; set; }
    protected abstract FilePatcher_Patch[]? RemoveDiscCheckPatches { get; }

    public abstract bool HasControllerConfig { get; }
    public abstract bool HasNetworkConfig { get; }
    public abstract bool Has16BitTextures { get; }

    #endregion

    #region Public Properties

    protected UbisoftIniAppDataManager<TAppData> AppDataManager { get; }
    public GraphicsModeSelectionViewModel GraphicsMode { get; }
    public string[] ModemQualityOptions { get; }
    public bool CanModifyGameFiles { get; }
    public bool CanRemoveDiscCheck { get; set; }
    public bool IsDiscPatchOutdated { get; set; }

    public bool FullscreenMode { get; set; }
    public bool TriLinear { get; set; }
    public bool TnL { get; set; }
    public bool IsTextures32Bit { get; set; }
    public bool CompressedTextures { get; set; }
    public int ReverseVideoQuality
    {
        get => 4 - VideoQuality;
        set => VideoQuality = 4 - value;
    }
    public int VideoQuality { get; set; }
    public bool AutoVideoQuality { get; set; }
    public bool IsVideo32Bpp { get; set; }
    public TLanguage CurrentLanguage { get; set; }
    public bool ControllerSupport { get; set; }
    public bool IsDiscCheckRemoved { get; set; }
    public int VerticalAxis { get; set; }
    public int HorizontalAxis { get; set; }
    public int ModemQualityIndex { get; set; }

    // Options from save file
    public R3SaveFile? SaveFile { get; set; }
    public string? SaveFileName { get; set; }
    public Context? SaveFileContext { get; set; }
    public bool JoypadRumbleRace { get; set; }
    public bool JoypadRumbleBattle { get; set; }

    #endregion

    #region Private Methods

    private FileSystemPath GetDinputFilePath()
    {
        return GameInstallation.InstallLocation.Directory + "dinput8.dll";
    }

    private DinputType GetDinputType()
    {
        FileSystemPath path = GetDinputFilePath();

        if (!path.FileExists)
            return DinputType.None;

        try
        {
            long size = path.GetSize();

            if (size == 156160)
                return DinputType.Controller;

            // If the size equals that of the Rayman 2 dinput file, delete it
            // as the Rayman 2 dinput file was accidentally used prior to version 4.1.2
            if (size == 66560)
            {
                Services.File.DeleteFile(path);
                return DinputType.None;
            }

            return DinputType.Unknown;
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Getting {0} dinput file size", GameInstallation.FullId);
            return DinputType.Unknown;
        }
    }

    #endregion

    #region Protected Methods

    protected abstract void LoadAppData();
    protected abstract void SaveAppData();

    protected override async Task LoadAsync()
    {
        AddSettingsLocation(LinkItemViewModel.LinkType.File, AppDataManager.AppDataFilePath);
        AddSettingsLocation(LinkItemViewModel.LinkType.File, AppDataManager.AppDataFilePath.GetVirtualStorePath());

        await AppDataManager.EnableUbiIniWriteAccessAsync();

        // Load the app data
        AppDataManager.Load();

        GraphicsMode.GetAvailableResolutions();

        LoadAppData();

        // Get the current dinput type
        DinputType dinputType = CanModifyGameFiles ? GetDinputType() : DinputType.Unknown;

        Logger.Info("The dinput type has been retrieved as {0}", dinputType);

        ControllerSupport = dinputType == DinputType.Controller;

        bool isAppliedDiscCheckPatchOutdated = false;

        // Check if the disc check has been removed
        FilePatcher_Patch[]? patches = RemoveDiscCheckPatches;
        CanRemoveDiscCheck = patches != null;

        if (CanRemoveDiscCheck)
        {
            DirectoryProgramInstallationStructure programStructure = GameInstallation.GameDescriptor.GetStructure<DirectoryProgramInstallationStructure>();
            FileSystemPath gameFile = programStructure.FileSystem.GetAbsolutePath(GameInstallation, ProgramPathType.PrimaryExe);

            // Check if it exists
            if (gameFile.FileExists)
            {
                Patcher = new FilePatcher(gameFile, patches);

                FilePatcher.PatchState patchState = Patcher.GetPatchState();

                if (patchState == null)
                {
                    CanRemoveDiscCheck = false;

                    Logger.Info("The game disc checker status could not be read");
                }
                else if (!patchState.IsPatched)
                {
                    IsDiscCheckRemoved = false;
                    CanRemoveDiscCheck = true;

                    Logger.Info("The game has not been modified to remove the disc checker");
                }
                else if (patchState.IsPatched)
                {
                    IsDiscCheckRemoved = true;
                    CanRemoveDiscCheck = true;

                    Logger.Info("The game has been modified to remove the disc checker");

                    if (patchState.IsVersionOutdated)
                    {
                        Logger.Info("The applied disc checker patch version is currently outdated");
                        isAppliedDiscCheckPatchOutdated = true;
                    }
                }
            }
            else
            {
                CanRemoveDiscCheck = false;
                Logger.Info("The game file was not found");
            }
        }
        else
        {
            Logger.Trace("The disc checker can not be removed for this game");
        }

        IsDiscPatchOutdated = isAppliedDiscCheckPatchOutdated;

        UnsavedChanges = isAppliedDiscCheckPatchOutdated;
    }

    protected override Task SaveAsync()
    {
        // Update the config data
        SaveAppData();

        // Save the config data
        AppDataManager.Save();

        // Copy data to virtual store
        FileSystemPath virtualStorePath = AppDataManager.AppDataFilePath.GetVirtualStorePath();
        try
        {
            // Copy the entire file
            Services.File.CopyFile(AppDataManager.AppDataFilePath, virtualStorePath, true);
        }
        catch (Exception ex)
        {
            Logger.Error(ex, "Copying {0} ubi.ini data to virtual store", GameInstallation.FullId);
        }

        if (CanModifyGameFiles)
        {
            try
            {
                // Get the current dinput type
                DinputType dinputType = GetDinputType();
                FileSystemPath dinputFilePath = GetDinputFilePath();

                Logger.Info("The dinput type has been retrieved as {0}", dinputType);

                if (ControllerSupport)
                {
                    if (dinputType != DinputType.Controller)
                    {
                        // Attempt to delete existing dinput file
                        if (dinputType != DinputType.None)
                            Services.File.DeleteFile(dinputFilePath);

                        // Write controller patch
                        File.WriteAllBytes(dinputFilePath, Files.dinput8_controller);
                    }
                }
                else if (dinputType == DinputType.Controller)
                {
                    // Attempt to delete existing dinput file
                    Services.File.DeleteFile(dinputFilePath);
                }

            }
            catch (Exception ex)
            {
                Logger.Error(ex, "Saving {0} dinput hack data", GameInstallation.FullId);
                throw;
            }

            if (CanRemoveDiscCheck)
            {
                try
                {
                    DirectoryProgramInstallationStructure programStructure = GameInstallation.GameDescriptor.GetStructure<DirectoryProgramInstallationStructure>();
                    FileSystemPath exeFile = programStructure.FileSystem.GetAbsolutePath(GameInstallation, ProgramPathType.PrimaryExe);

                    Patcher = new FilePatcher(exeFile, RemoveDiscCheckPatches);

                    FilePatcher.PatchState patchState = Patcher.GetPatchState();

                    if (patchState != null)
                    {
                        // Apply patch if set to do so and not already patched or the current patch is outdated
                        if ((!patchState.IsPatched || patchState.IsVersionOutdated) && IsDiscCheckRemoved)
                            Patcher.PatchFile(true);
                        // Revert patch if set to do so and currently patched
                        else if (patchState.IsPatched && !IsDiscCheckRemoved)
                            Patcher.PatchFile(false);
                    }

                    IsDiscPatchOutdated = false;
                }
                catch (Exception ex)
                {
                    Logger.Error(ex, "Saving {0} disc check modification", GameInstallation.FullId);
                    throw;
                }
            }
        }

        return Task.CompletedTask;
    }

    protected override void SettingsPropertyChanged(string propertyName)
    {
        if (propertyName is
            nameof(FullscreenMode) or
            nameof(TriLinear) or
            nameof(TnL) or
            nameof(IsTextures32Bit) or
            nameof(CompressedTextures) or
            nameof(VideoQuality) or
            nameof(AutoVideoQuality) or
            nameof(IsVideo32Bpp) or
            nameof(CurrentLanguage) or
            nameof(ControllerSupport) or
            nameof(IsDiscCheckRemoved) or
            nameof(JoypadRumbleRace) or
            nameof(JoypadRumbleBattle) or
            nameof(VerticalAxis) or
            nameof(HorizontalAxis) or
            nameof(ModemQualityIndex))
        {
            UnsavedChanges = true;
        }
    }

    protected void LoadMArenaSaveFile()
    {
        FileSystemPath saveFileDir = GameInstallation.InstallLocation.Directory + "MENU" + "SaveGame";
        const string saveFileName = "raymanm.sav";

        // Make sure the save file exists
        if (!(saveFileDir + saveFileName).FileExists)
            return;

        // Create the context to use
        SaveFileContext = new RCPContext(saveFileDir);
        SaveFileContext.Initialize(GameInstallation);
        SaveFileName = saveFileName;
        SaveFileContext.AddFile(new LinearFile(SaveFileContext, SaveFileName));

        // Read the file
        using (SaveFileContext)
            SaveFile = FileFactory.Read<R3SaveFile>(SaveFileContext, SaveFileName);

        // Get the settings
        JoypadRumbleRace = SaveFile.Elements.FirstOrDefault(x => x.ElementName == "sg_iracepadrumble")?.Values?.FirstOrDefault()?.IntegerValue == 1;
        JoypadRumbleBattle = SaveFile.Elements.FirstOrDefault(x => x.ElementName == "sg_ishootpadrumble")?.Values?.FirstOrDefault()?.IntegerValue == 1;
    }

    protected void SaveMArenaSaveFile()
    {
        if (SaveFileContext == null || SaveFileName == null || SaveFile == null)
            return;

        R3SaveElement? raceElement = SaveFile.Elements.FirstOrDefault(x => x.ElementName == "sg_iracepadrumble");
        if (raceElement != null)
            foreach (R3SaveValue value in raceElement.Values)
                value.IntegerValue = (uint)(JoypadRumbleRace ? 1 : 0);

        R3SaveElement? battleElement = SaveFile.Elements.FirstOrDefault(x => x.ElementName == "sg_ishootpadrumble");
        if (battleElement != null)
            foreach (R3SaveValue value in battleElement.Values)
                value.IntegerValue = (uint)(JoypadRumbleBattle ? 1 : 0);

        using (SaveFileContext)
            FileFactory.Write(SaveFileContext, SaveFileName, SaveFile);
    }

    #endregion

    #region Enums

    /// <summary>
    /// The available types of the dinput8.dll file
    /// </summary>
    private enum DinputType
    {
        None,
        Controller,
        Unknown
    }

    #endregion
}