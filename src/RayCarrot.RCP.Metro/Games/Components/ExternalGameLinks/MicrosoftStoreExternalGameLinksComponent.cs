namespace RayCarrot.RCP.Metro.Games.Components;

public class MicrosoftStoreExternalGameLinksComponent : ExternalGameLinksComponent
{
    public MicrosoftStoreExternalGameLinksComponent(string productId) : base(_ => GetExternalGameLinks(productId)) { }

    private static IEnumerable<GameUriLink> GetExternalGameLinks(string productId) => new[]
    {
        new GameUriLink(
            Header: new ResourceLocString(nameof(Resources.GameDisplay_OpenInWinStore)),
            Uri: MicrosoftStoreHelpers.GetStorePageURI(productId),
            AssetIcon: WebIconAsset.MicrosoftStore)
    };
}