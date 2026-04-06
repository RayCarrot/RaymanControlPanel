using RayCarrot.RCP.Metro.Games.Components;
using RayCarrot.RCP.Metro.Games.Finder;

namespace RayCarrot.RCP.Metro.Games.Clients.MGba;

public sealed class BizHawkGameClientDescriptor : EmulatorGameClientDescriptor
{
    #region Public Properties

    public override string GameClientId => "BizHawk";
    public override bool InstallationRequiresFile => true;
    public override GamePlatform[] SupportedPlatforms => new[] { GamePlatform.Snes, GamePlatform.Ps1, GamePlatform.Gbc, GamePlatform.Gba };
    public override LocalizedString DisplayName => new ResourceLocString(nameof(Resources.GameClients_BizHawk));
    public override GameClientIconAsset Icon => GameClientIconAsset.BizHawk;

    #endregion

    #region Public Methods

    public override void RegisterComponents(IGameComponentBuilder builder)
    {
        base.RegisterComponents(builder);

        builder.Register<LaunchGameComponent, DefaultGameClientLaunchGameComponent>();
        builder.Register<EmulatedSaveFilesComponent, BizHawkEmulatedSaveFilesComponent>();
    }

    public override FinderQuery[] GetFinderQueries() => new FinderQuery[]
    {
        new Win32ShortcutFinderQuery("bizhawk") { FileName = "EmuHawk.exe" },
        new Win32ShortcutFinderQuery("emuhawk") { FileName = "EmuHawk.exe" },
    };

    #endregion
}