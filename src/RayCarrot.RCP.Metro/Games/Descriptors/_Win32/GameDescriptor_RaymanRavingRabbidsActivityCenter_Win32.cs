using RayCarrot.RCP.Metro.Games.Components;
using RayCarrot.RCP.Metro.Games.Structure;

namespace RayCarrot.RCP.Metro;

/// <summary>
/// The Rayman Raving Rabbids Activity Center (Win32) game descriptor
/// </summary>
public sealed class GameDescriptor_RaymanRavingRabbidsActivityCenter_Win32 : Win32GameDescriptor
{
    #region Public Properties

    public override string GameId => "RaymanRavingRabbidsActivityCenter_Win32";
    public override string LegacyGameId => "RaymanRavingRabbidsActivityCenter";
    public override Game Game => Game.RaymanRavingRabbidsActivityCenter;
    public override GameCategory Category => GameCategory.Other;

    public override LocalizedString DisplayName => new ResourceLocString(nameof(Resources.RaymanRavingRabbidsActivityCenter_Win32_Title));
    public override DateTime ReleaseDate => new(2006, 10, 19);

    public override GameIconAsset Icon => GameIconAsset.RaymanRavingRabbidsActivityCenter;
    public override GameBannerAsset Banner => GameBannerAsset.RaymanRavingRabbids;

    #endregion

    #region Private Methods

    private static async Task ShowLaunchMessageAsync(GameInstallation gameInstallation)
    {
        // Check if the launch message should show
        if (!gameInstallation.GetValue<bool>(GameDataKey.RRRAC_ShownLaunchMessage))
        {
            await Services.MessageUI.DisplayMessageAsync(Resources.RabbidsActivityCenter_LaunchMessage, MessageType.Information);

            // Flag that the message should not be shown again
            gameInstallation.SetValue(GameDataKey.RRRAC_ShownLaunchMessage, true);
        }
    }

    #endregion

    #region Protected Methods

    protected override void RegisterComponents(IGameComponentBuilder builder)
    {
        base.RegisterComponents(builder);

        builder.Register(new OnGameLaunchedComponent(ShowLaunchMessageAsync));

        builder.Register(new DiscordRichPresenceComponent("Rayman Raving Rabbids Activity Center", "rayman_raving_rabbids_activity_center"));
    }

    protected override ProgramInstallationStructure CreateStructure() => new DirectoryProgramInstallationStructure(new ProgramFileSystem(new ProgramPath[]
    {
        // Files
        new ProgramFilePath("Rayman.exe", ProgramPathType.PrimaryExe, required: true),
    }));

    #endregion

    #region Public Methods

    public override IEnumerable<GameAddAction> GetAddActions() => new GameAddAction[]
    {
        new LocateDirectoryGameAddAction(this),
    };

    #endregion
}