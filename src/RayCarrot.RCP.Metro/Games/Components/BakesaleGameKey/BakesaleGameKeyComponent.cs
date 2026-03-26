namespace RayCarrot.RCP.Metro.Games.Components;

[GameComponentBase]
public class BakesaleGameKeyComponent : GameComponent
{
    public BakesaleGameKeyComponent(uint gameKey)
    {
        GameKey = gameKey;
    }

    public uint GameKey { get; }
}