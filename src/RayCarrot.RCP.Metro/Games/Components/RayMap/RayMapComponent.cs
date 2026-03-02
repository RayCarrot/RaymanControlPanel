namespace RayCarrot.RCP.Metro.Games.Components;

/// <summary>
/// Defines information for the RayMap map viewer
/// </summary>
[GameComponentBase]
public class RayMapComponent : GameComponent
{
    public RayMapComponent(RayMapViewer viewer, string mode, string folder, string? vol = null)
    {
        Viewer = viewer;
        Mode = mode;
        Folder = folder;
        Vol = vol;
    }

    public RayMapViewer Viewer { get; }
    public string Mode { get; }
    public string Folder { get; }
    public string? Vol { get; }

    public string GetURL() => Viewer switch
    {
        RayMapViewer.RayMap => AppURLs.GetRayMapGameURL(Mode, Folder),
        RayMapViewer.Ray1Map => AppURLs.GetRay1MapGameURL(Mode, Folder, Vol),
        _ => throw new ArgumentOutOfRangeException(nameof(Viewer), Viewer, null)
    };

    public WebIconAsset GetIcon() => Viewer switch
    {
        RayMapViewer.RayMap => WebIconAsset.RayMap,
        RayMapViewer.Ray1Map => WebIconAsset.Ray1Map,
        _ => throw new ArgumentOutOfRangeException(nameof(Viewer), Viewer, null)
    };

    public enum RayMapViewer { RayMap, Ray1Map }
}