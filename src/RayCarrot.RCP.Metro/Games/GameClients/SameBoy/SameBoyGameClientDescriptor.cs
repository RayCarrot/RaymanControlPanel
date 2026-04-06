using RayCarrot.RCP.Metro.Games.Components;
using RayCarrot.RCP.Metro.Games.Finder;

namespace RayCarrot.RCP.Metro.Games.Clients.MGba;

public sealed class SameBoyGameClientDescriptor : EmulatorGameClientDescriptor
{
    #region Public Properties

    public override string GameClientId => "SameBoy";
    public override bool InstallationRequiresFile => true;
    public override GamePlatform[] SupportedPlatforms => new[] { GamePlatform.Gbc };
    public override LocalizedString DisplayName => new ResourceLocString(nameof(Resources.GameClients_SameBoy));
    public override GameClientIconAsset Icon => GameClientIconAsset.SameBoy;

    #endregion

    #region Public Methods

    public override void RegisterComponents(IGameComponentBuilder builder)
    {
        base.RegisterComponents(builder);

        builder.Register<LaunchGameComponent, DefaultGameClientLaunchGameComponent>();
        builder.Register<EmulatedSaveFilesComponent, SameBoyEmulatedSaveFilesComponent>();
    }

    public override FinderQuery[] GetFinderQueries() => new FinderQuery[]
    {
        new Win32ShortcutFinderQuery("SameBoy") { FileName = "sameboy.exe" },
    };

    #endregion
}