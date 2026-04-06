namespace RayCarrot.RCP.Metro;

[AssetDirectory($"{Assets.AssetsPath}/GamePlatformIcons")]
public enum GamePlatformIconAsset
{
    [AssetFileName("MSDOS.png")]
    MsDos,

    [AssetFileName("Win32.png")]
    Win32,

    [AssetFileName("WindowsPackage.png")]
    WindowsPackage,

    [AssetFileName("SNES.png")]
    Snes,

    [AssetFileName("Jaguar.png")]
    Jaguar,

    [AssetFileName("Gbc.png")]
    Gbc,

    [AssetFileName("PlayStation.png")]
    Ps1,

    [AssetFileName("PlayStation2.png")]
    Ps2,

    [AssetFileName("GameCube.png")]
    GameCube,

    [AssetFileName("Gba.png")]
    Gba,
}