namespace RayCarrot.RCP.Metro.Games.Components;

/// <summary>
/// This component is to be used on games which can use <see cref="Clients.Steam.SteamGameClientDescriptor"/>
/// </summary>
[GameComponentBase(SingleInstance = true)]
public class SteamGameClientComponent : GameComponent
{
    public SteamGameClientComponent(string steamId, bool usesSteamStubDrm = false)
    {
        SteamId = steamId;
        UsesSteamStubDrm = usesSteamStubDrm;
    }

    public string SteamId { get; }
    public bool UsesSteamStubDrm { get; }
}