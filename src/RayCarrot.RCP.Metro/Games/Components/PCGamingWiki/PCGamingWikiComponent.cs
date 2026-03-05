namespace RayCarrot.RCP.Metro.Games.Components;

[GameComponentBase]
public class PCGamingWikiComponent : GameComponent
{
    public PCGamingWikiComponent(string pageName)
    {
        PageName = pageName;
    }
 
    public string PageName { get; }

    private static IEnumerable<GameLinksComponent.GameUriLink> GetExternalUriLinks(GameInstallation gameInstallation)
    {
        string pageName = gameInstallation.GetRequiredComponent<PCGamingWikiComponent>().PageName;

        return new[]
        {
            new GameLinksComponent.GameUriLink(
                Header: new ResourceLocString(nameof(Resources.GameDisplay_OpenPCGamingWikiPage)),
                Uri: $"https://www.pcgamingwiki.com/wiki/{pageName}",
                AssetIcon: WebIconAsset.PCGamingWiki),
        };
    }

    public override void RegisterComponents(IGameComponentBuilder builder)
    {
        base.RegisterComponents(builder);

        builder.Register(new ExternalGameLinksComponent(GetExternalUriLinks));
    }
}