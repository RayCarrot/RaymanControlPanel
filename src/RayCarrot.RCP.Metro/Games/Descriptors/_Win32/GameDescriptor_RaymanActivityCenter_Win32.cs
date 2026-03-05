using RayCarrot.RCP.Metro.Games.Components;
using RayCarrot.RCP.Metro.Games.Structure;

namespace RayCarrot.RCP.Metro;

/// <summary>
/// The Rayman Activity Center (Win32) game descriptor
/// </summary>
public sealed class GameDescriptor_RaymanActivityCenter_Win32 : Win32GameDescriptor
{
    #region Public Properties

    public override string GameId => "RaymanActivityCenter_Win32";
    public override string LegacyGameId => "RaymanActivityCenter";
    public override Game Game => Game.RaymanActivityCenter;
    public override GameCategory Category => GameCategory.Other;

    public override LocalizedString DisplayName => new ResourceLocString(nameof(Resources.RaymanActivityCenter_Win32_Title));
    public override DateTime ReleaseDate => new(1999, 01, 01); // Not exact

    public override GameIconAsset Icon => GameIconAsset.RaymanActivityCenter;
    public override GameBannerAsset Banner => GameBannerAsset.RaymanActivityCenter;

    #endregion

    #region Protected Methods

    protected override void RegisterComponents(IGameComponentBuilder builder)
    {
        base.RegisterComponents(builder);

        builder.Register(new PCGamingWikiComponent("Rayman_Activity_Center"));

        builder.Register(new DiscordRichPresenceComponent("Rayman Activity Center", "rayman_activity_center"));
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