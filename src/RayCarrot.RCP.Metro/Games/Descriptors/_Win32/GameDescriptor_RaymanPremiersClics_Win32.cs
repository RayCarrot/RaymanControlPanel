using RayCarrot.RCP.Metro.Games.Components;
using RayCarrot.RCP.Metro.Games.Structure;

namespace RayCarrot.RCP.Metro;

/// <summary>
/// The Rayman Premiers Clics (Win32) game descriptor
/// </summary>
public sealed class GameDescriptor_RaymanPremiersClics_Win32 : Win32GameDescriptor
{
    #region Public Properties

    public override string GameId => "RaymanPremiersClics_Win32";
    public override string LegacyGameId => "RaymanPremiersClics";
    public override Game Game => Game.RaymanPremiersClics;
    public override GameCategory Category => GameCategory.Other;

    public override LocalizedString DisplayName => new ResourceLocString(nameof(Resources.RaymanPremiersClics_Win32_Title));
    public override DateTime ReleaseDate => new(2001, 01, 01); // Not exact

    public override GameIconAsset Icon => GameIconAsset.RaymanPremiersClics;
    public override GameBannerAsset Banner => GameBannerAsset.RaymanPremiersClics;

    #endregion

    #region Protected Methods

    protected override void RegisterComponents(IGameComponentBuilder builder)
    {
        base.RegisterComponents(builder);

        builder.Register(new PCGamingWikiComponent("Rayman_Premiers_Clics"));

        builder.Register(new DiscordRichPresenceComponent("Rayman Premiers Clics", "rayman_premier_clics"));
    }

    protected override ProgramInstallationStructure CreateStructure() => new DirectoryProgramInstallationStructure(new ProgramFileSystem(new ProgramPath[]
    {
        // Files
        new ProgramFilePath("RAYMAN.exe", ProgramPathType.PrimaryExe, required: true),
    }));

    #endregion

    #region Public Methods

    public override IEnumerable<GameAddAction> GetAddActions() => new GameAddAction[]
    {
        new LocateDirectoryGameAddAction(this),
    };

    #endregion
}