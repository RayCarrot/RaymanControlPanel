using RayCarrot.RCP.Metro.Games.Components;
using RayCarrot.RCP.Metro.Games.Structure;

namespace RayCarrot.RCP.Metro;

/// <summary>
/// The Rayman 1 minigames (Win32) game descriptor
/// </summary>
public sealed class GameDescriptor_Rayman1Minigames_Win32 : Win32GameDescriptor
{
    #region Public Override Properties

    public override string GameId => "Rayman1Minigames_Win32";
    public override string LegacyGameId => "Ray1Minigames";
    public override Game Game => Game.Rayman1Minigames;
    public override GameCategory Category => GameCategory.Other;

    public override LocalizedString DisplayName => new ResourceLocString(nameof(Resources.Rayman1Minigames_Win32_Title));
    public override DateTime ReleaseDate => new(1999, 07, 20);

    public override GameIconAsset Icon => GameIconAsset.Rayman1Minigames;
    public override GameBannerAsset Banner => GameBannerAsset.Rayman1;

    #endregion

    #region Protected Methods

    protected override void RegisterComponents(IGameComponentBuilder builder)
    {
        base.RegisterComponents(builder);

        builder.Register<OnGameAddedComponent, DefaultToRunAsAdminOnGameAddedComponent>();

        // Doesn't work since it's a different executable, TODO: Allow defining a separate exe for some games
        // builder.Register(new DiscordRichPresenceComponent("Rayman Minigames", "rayman_minigames"));
    }

    protected override ProgramInstallationStructure CreateStructure() => new DirectoryProgramInstallationStructure(new ProgramFileSystem(new ProgramPath[]
    {
        // Files
        new ProgramFilePath("RayGames.exe", ProgramPathType.PrimaryExe, required: true),
    }));

    #endregion

    #region Public Methods

    public override IEnumerable<GameAddAction> GetAddActions() => new GameAddAction[]
    {
        new LocateDirectoryGameAddAction(this)
    };

    #endregion
}