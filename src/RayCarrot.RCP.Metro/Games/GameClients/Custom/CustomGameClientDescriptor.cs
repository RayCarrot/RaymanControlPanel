using RayCarrot.RCP.Metro.Games.Components;

namespace RayCarrot.RCP.Metro.Games.Clients.Custom;

public sealed class CustomGameClientDescriptor : EmulatorGameClientDescriptor
{
    #region Public Properties

    public override string GameClientId => "Custom";
    public override bool InstallationRequiresFile => true;
    public override GamePlatform[] SupportedPlatforms => 
    [
        GamePlatform.MsDos, 
        GamePlatform.Jaguar, 
        GamePlatform.Ps1, 
        GamePlatform.Ps2, 
        GamePlatform.Snes, 
        GamePlatform.Gbc, 
        GamePlatform.Gba, 
        GamePlatform.GameCube
    ];
    public override LocalizedString DisplayName => new ResourceLocString(nameof(Resources.GameClients_Custom));
    public override GameClientIconAsset Icon => GameClientIconAsset.Custom;

    #endregion

    #region Public Methods

    public override void RegisterComponents(IGameComponentBuilder builder)
    {
        base.RegisterComponents(builder);

        builder.Register<LaunchGameComponent, CustomGameClientLaunchGameComponent>();
    }

    public override GameClientOptionsViewModel GetGameClientOptionsViewModel(GameClientInstallation gameClientInstallation) =>
        new CustomGameClientOptionsViewModel(gameClientInstallation);

    public override async Task OnGameClientAddedAsync(GameClientInstallation gameClientInstallation)
    {
        await base.OnGameClientAddedAsync(gameClientInstallation);

        gameClientInstallation.SetValue(GameClientDataKey.Custom_LaunchArgs, "\"%game%\"");
    }

    #endregion
}