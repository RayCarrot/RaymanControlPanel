namespace RayCarrot.RCP.Metro.Games.Components;

[GameComponentBase]
[GameComponentInstance(SingleInstance = true)]
public class DiscordRichPresenceComponent : GameComponent
{
    public DiscordRichPresenceComponent(string displayName, string imageKey)
    {
        DisplayName = displayName;
        ImageKey = imageKey;
    }

    public string DisplayName { get; }
    public string ImageKey { get; }
}