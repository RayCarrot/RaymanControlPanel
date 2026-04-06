namespace RayCarrot.RCP.Metro;

/// <summary>
/// A game descriptor for a SNES program
/// </summary>
public abstract class SnesGameDescriptor : GameDescriptor
{
    #region Public Properties

    public override GamePlatform Platform => GamePlatform.Snes;
    public override bool DefaultToUseGameClient => true;

    #endregion
}