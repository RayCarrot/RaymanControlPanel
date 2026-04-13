using RayCarrot.RCP.Metro.Games.Components;
using RayCarrot.RCP.Metro.Games.Finder;

namespace RayCarrot.RCP.Metro.Games.Clients.MGba;

public sealed class BsnesGameClientDescriptor : EmulatorGameClientDescriptor
{
    #region Public Properties

    public override string GameClientId => "bsnes";
    public override bool InstallationRequiresFile => true;
    public override GamePlatform[] SupportedPlatforms => new[] { GamePlatform.Snes };
    public override LocalizedString DisplayName => new ResourceLocString(nameof(Resources.GameClients_Bsnes));
    public override GameClientIconAsset Icon => GameClientIconAsset.Bsnes;

    #endregion

    #region Public Methods

    public override void RegisterComponents(IGameComponentBuilder builder)
    {
        base.RegisterComponents(builder);

        builder.Register<LaunchGameComponent, DefaultGameClientLaunchGameComponent>();
    }

    public override FinderQuery[] GetFinderQueries() => new FinderQuery[]
    {
        new Win32ShortcutFinderQuery("bsnes") { FileName = "bsnes.exe" },
    };

    #endregion
}