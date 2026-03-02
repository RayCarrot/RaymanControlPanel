namespace RayCarrot.RCP.Metro.Games.Components;

public abstract class GameLinksComponent : FactoryGameComponent<IEnumerable<GameLinksComponent.GameUriLink>>
{
    protected GameLinksComponent(Func<GameInstallation, IEnumerable<GameUriLink>> objFactory) : base(objFactory) { }

    /// <summary>
    /// A game uri link for local and external locations
    /// </summary>
    /// <param name="Header">The link header</param>
    /// <param name="Uri">The link uri</param>
    /// <param name="Icon">An optional icon</param>
    /// <param name="AssetIcon">An optional asset icon</param>
    /// <param name="Arguments">Optional arguments if it's a local file</param>
    public record GameUriLink(
        LocalizedString Header,
        string Uri,
        GenericIconKind Icon = GenericIconKind.None,
        Enum? AssetIcon = null,
        string? Arguments = null);
}