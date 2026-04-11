using Newtonsoft.Json;

namespace RayCarrot.RCP.Metro.ModLoader;

[JsonConverter(typeof(ModFilePathJsonConverter))]
public readonly record struct ModFilePath
{
    public ModFilePath(string filePath)
    {
        FilePath = filePath;
        Location = String.Empty;
        LocationID = String.Empty;
    }
    public ModFilePath(string filePath, string location, string locationId)
    {
        FilePath = filePath;
        Location = location;
        LocationID = locationId;
    }

    public string FilePath { get; }
    public string Location { get; }
    public string LocationID { get; }

    public bool HasLocation => Location != String.Empty;
    public string FullFilePath => HasLocation ? System.IO.Path.Combine(Location, FilePath) : FilePath;

    public override string ToString() => HasLocation ? $"{Location}:{FilePath}" : FilePath;
}