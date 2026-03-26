using RayCarrot.RCP.Metro.Archive;
using RayCarrot.RCP.Metro.Archive.Bakesale;

namespace RayCarrot.RCP.Metro.Games.Components;

[RequiredGameComponents(typeof(BakesaleGameKeyComponent))]
public class BakesaleArchiveComponent : ArchiveComponent
{
    public BakesaleArchiveComponent() : base(GetArchiveManager, GetArchiveFilePaths, Id) { }

    public new const string Id = "BAKESALE_PIE";

    private static IArchiveDataManager GetArchiveManager(GameInstallation gameInstallation)
    {
        uint gameKey = gameInstallation.GetRequiredComponent<BakesaleGameKeyComponent>().GameKey;
        return new BakesalePieArchiveDataManager(gameKey);
    }

    private static IEnumerable<string> GetArchiveFilePaths(GameInstallation gameInstallation)
    {
        return ["assets.pie"];
    }
}