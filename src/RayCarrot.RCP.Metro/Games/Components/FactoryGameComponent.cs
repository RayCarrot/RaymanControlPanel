namespace RayCarrot.RCP.Metro.Games.Components;

/// <summary>
/// A game component which provides an object factory of a specific type
/// </summary>
/// <typeparam name="T">The type of objects to create</typeparam>
public abstract class FactoryGameComponent<T> : GameComponent
{
    protected FactoryGameComponent(Func<GameInstallation, T> objFactory)
    {
        _objFactory = objFactory;
    }

    private readonly Func<GameInstallation, T> _objFactory;

    public int Priority { get; init; }

    public virtual T CreateObject() => _objFactory(GameInstallation);
}

/// <summary>
/// A game component which provides an object factory of a specific type with a parameter
/// </summary>
/// <typeparam name="P1">The type of the first parameter</typeparam>
/// <typeparam name="T">The type of objects to create</typeparam>
public abstract class FactoryGameComponent<P1, T> : GameComponent
{
    protected FactoryGameComponent(Func<GameInstallation, P1, T> objFactory)
    {
        _objFactory = objFactory;
    }

    private readonly Func<GameInstallation, P1, T> _objFactory;

    public int Priority { get; init; }

    public virtual T CreateObject(P1 p1) => _objFactory(GameInstallation, p1);
}