using System.Diagnostics;
using RayCarrot.RCP.Metro.Games.RichPresence;

namespace RayCarrot.RCP.Metro.Games.Components;

// TODO: Add GameFeature attribute?
[GameComponentBase]
public class RichPresenceManagerComponent : FactoryGameComponent<Process, GameRichPresenceManager>
{
    public RichPresenceManagerComponent(Func<GameInstallation, Process, GameRichPresenceManager> objFactory) : base(objFactory) { }
}