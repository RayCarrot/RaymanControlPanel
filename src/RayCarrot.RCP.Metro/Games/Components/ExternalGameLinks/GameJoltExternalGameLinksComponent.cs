namespace RayCarrot.RCP.Metro.Games.Components;

public class GameJoltExternalGameLinksComponent : ExternalGameLinksComponent
{
    public GameJoltExternalGameLinksComponent(string url) : base(_ => GetExternalGameLinks(url)) { }

    private static IEnumerable<GameUriLink> GetExternalGameLinks(string url) => new[]
    {
        new GameUriLink(
            Header: new ResourceLocString(nameof(Resources.GameDisplay_OpenGameJoltPage)),
            Uri: url,
            AssetIcon: WebIconAsset.GameJolt)
    };
}