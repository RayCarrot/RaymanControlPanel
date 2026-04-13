using System.IO;

namespace RayCarrot.RCP.Metro;

// IDEA: Move all deployable files to common folder Temp\Deployable or perhaps in the user's temp instead of the app's temp?

/// <summary>
/// Common paths used in the Rayman Control Panel
/// </summary>
public static class AppFilePaths
{
    /// <summary>
    /// The base user data directory
    /// </summary>
    public static FileSystemPath UserDataBaseDir => Environment.SpecialFolder.LocalApplicationData.GetFolderPath() + "Rayman Control Panel" + "RCP_Metro";

    /// <summary>
    /// The <see cref="AppUserData"/> file path
    /// </summary>
    public static FileSystemPath AppUserDataPath => UserDataBaseDir + "appuserdata.json";

    /// <summary>
    /// The log file path
    /// </summary>
    public static FileSystemPath LogFile => UserDataBaseDir + "Temp\\Log.txt";

    /// <summary>
    /// The archive log file path
    /// </summary>
    public static FileSystemPath ArchiveLogFile => UserDataBaseDir + "Temp\\Log_archived.txt";

    /// <summary>
    /// The installation temp directory
    /// </summary>
    public static FileSystemPath InstallTempPath => UserDataBaseDir + "Temp\\Installation";

    /// <summary>
    /// The updater file path
    /// </summary>
    public static FileSystemPath UpdaterFilePath => InstallTempPath + "Updater.exe";

    /// <summary>
    /// The uninstaller file path
    /// </summary>
    public static FileSystemPath UninstallFilePath => InstallTempPath + "Uninstaller.exe";

    /// <summary>
    /// The icon files path
    /// </summary>
    public static FileSystemPath IconsPath => UserDataBaseDir + "Icons";

    /// <summary>
    /// The game assets path
    /// </summary>
    public static FileSystemPath GameAssetsPath => UserDataBaseDir + "GameAssets";

    /// <summary>
    /// The temporary files path
    /// </summary>
    public static FileSystemPath TempPath => Path.Combine(Path.GetTempPath(), "RCP_Metro");

    /// <summary>
    /// The common path to the ubi.ini file
    /// </summary>
    public static FileSystemPath UbiIniPath => Environment.SpecialFolder.Windows.GetFolderPath() + @"Ubisoft\ubi.ini";

    /// <summary>
    /// The registry base key
    /// </summary>
    public const string RegistryBaseKey = @"HKEY_CURRENT_USER\Software\RayCarrot\RCP_Metro";

    /// <summary>
    /// The license accepted value name
    /// </summary>
    public const string RegistryLicenseValue = "LicenseAccepted";

    /// <summary>
    /// The Rayman Raving Rabbids registry key path
    /// </summary>
    public const string RaymanRavingRabbidsRegistryKey = @"HKEY_CURRENT_USER\Software\Ubisoft\Rayman4";

    /// <summary>
    /// The Rayman Raving Rabbids 2 registry key path
    /// </summary>
    public const string RaymanRavingRabbids2RegistryKey = @"HKEY_CURRENT_USER\Software\Ubisoft\Rayman Raving Rabbids 2";

    /// <summary>
    /// The Rayman Origins registry key path
    /// </summary>
    public const string RaymanOriginsRegistryKey = @"HKEY_CURRENT_USER\Software\Ubisoft\RaymanOrigins";

    /// <summary>
    /// The Rayman Legends registry key path
    /// </summary>
    public const string RaymanLegendsRegistryKey = @"HKEY_CURRENT_USER\Software\Ubisoft\Rayman Legends";

    /// <summary>
    /// The RegEdit settings registry key path
    /// </summary>
    public const string RegeditRegistryKey = @"HKEY_CURRENT_USER\Software\Microsoft\Windows\CurrentVersion\Applets\Regedit";

    /// <summary>
    /// The Uninstall/Change programs registry key path
    /// </summary>
    public const string UninstallRegistryKey = @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall";

    /// <summary>
    /// The file extension for compressed backups
    /// </summary>
    public const string BackupCompressionExtension = ".rcpb";
}