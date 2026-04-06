namespace RayCarrot.RCP.Metro;

/// <summary>
/// Defines the platform a game is for
/// </summary>
public enum GamePlatform
{
    // NOTE: Order is PC first, then by release

    /// <summary>
    /// MS-DOS
    /// </summary>
    [GamePlatformInfo(nameof(Resources.Platform_MsDos), GamePlatformIconAsset.MsDos)]
    MsDos,

    /// <summary>
    /// Win32
    /// </summary>
    [GamePlatformInfo(nameof(Resources.Platform_Win32), GamePlatformIconAsset.Win32)]
    Win32,

    /// <summary>
    /// Windows package (.appx/.msix)
    /// </summary>
    [GamePlatformInfo(nameof(Resources.Platform_WindowsPackage), GamePlatformIconAsset.WindowsPackage)]
    WindowsPackage,

    /// <summary>
    /// Super Nintendo Entertainment System
    /// </summary>
    [GamePlatformInfo("Super Nintendo Entertainment System", GamePlatformIconAsset.Snes)] // 1990 // TODO-LOC
    Snes,

    /// <summary>
    /// Atari Jaguar
    /// </summary>
    [GamePlatformInfo(nameof(Resources.Platform_Jaguar), GamePlatformIconAsset.Jaguar)] // 1993
    Jaguar,

    /// <summary>
    /// PlayStation
    /// </summary>
    [GamePlatformInfo(nameof(Resources.Platform_Ps1), GamePlatformIconAsset.Ps1)] // 1994
    Ps1,

    /// <summary>
    /// Game Boy Color
    /// </summary>
    [GamePlatformInfo(nameof(Resources.Platform_Gbc), GamePlatformIconAsset.Gbc)] // 1998
    Gbc,

    /// <summary>
    /// PlayStation
    /// </summary>
    [GamePlatformInfo(nameof(Resources.Platform_Ps2), GamePlatformIconAsset.Ps2)] // 2000
    Ps2,

    /// <summary>
    /// Game Boy Advance
    /// </summary>
    [GamePlatformInfo(nameof(Resources.Platform_Gba), GamePlatformIconAsset.Gba)] // 2001
    Gba,

    /// <summary>
    /// GameCube
    /// </summary>
    [GamePlatformInfo(nameof(Resources.Platform_GameCube), GamePlatformIconAsset.GameCube)] // 2001
    GameCube,
}