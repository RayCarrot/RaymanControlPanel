using System.Globalization;
using System.IO;
using System.Text;
using Murmur;

namespace RayCarrot.RCP.Metro.ModLoader.Modules.BakesaleResource;

public readonly struct BakesaleResourceFile
{
    public BakesaleResourceFile(string filePathInResource, FileSystemPath sourceFilePath)
    {
        FilePathInResource = filePathInResource;
        SourceFilePath = sourceFilePath;
    }

    public string FilePathInResource { get; }
    public FileSystemPath SourceFilePath { get; }

    public uint GetResourceNameHash()
    {
        if (FilePathInResource.StartsWith("_unnamed"))
        {
            string fileName = Path.GetFileNameWithoutExtension(FilePathInResource);
            return UInt32.Parse(fileName, NumberStyles.HexNumber);
        }
        else
        {
            // Normalize the path separator
            string path = FilePathInResource.Replace('\\', '/');

            // Remove the file extension
            string pathWithoutExtension = Path.ChangeExtension(path, null);

            using Murmur32 hasher = MurmurHash.Create32();
            byte[] hash = hasher.ComputeHash(Encoding.UTF8.GetBytes(pathWithoutExtension));
            return BitConverter.ToUInt32(hash, 0);
        }
    }
}