namespace RayCarrot.RCP.Metro;

[Flags]
public enum PancakeMemoryCardDirectoryFlags : uint
{
    None = 0,
    Created = 1 << 0,
    InUse = 1 << 1,
}