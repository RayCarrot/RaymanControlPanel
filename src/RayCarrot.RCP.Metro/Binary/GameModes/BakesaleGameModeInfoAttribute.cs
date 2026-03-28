namespace RayCarrot.RCP.Metro;

[AttributeUsage(AttributeTargets.Field)]
public sealed class BakesaleGameModeInfoAttribute : GameModeBaseAttribute
{
    public BakesaleGameModeInfoAttribute(string displayName, uint gameKey) : base(displayName)
    {
        GameKey = gameKey;
    }

    public uint GameKey { get; }

    public override object? GetSettingsObject() => null;
}