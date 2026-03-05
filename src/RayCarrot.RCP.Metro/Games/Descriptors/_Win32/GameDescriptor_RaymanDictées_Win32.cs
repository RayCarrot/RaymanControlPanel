using RayCarrot.RCP.Metro.Games.Components;
using RayCarrot.RCP.Metro.Games.Structure;

namespace RayCarrot.RCP.Metro;

/// <summary>
/// The Rayman Dictées (Win32) game descriptor
/// </summary>
public sealed class GameDescriptor_RaymanDictées_Win32 : Win32GameDescriptor
{
    #region Public Properties

    public override string GameId => "RaymanDictées_Win32";
    public override string LegacyGameId => "RaymanDictées";
    public override Game Game => Game.RaymanDictées;
    public override GameCategory Category => GameCategory.Other;

    public override LocalizedString DisplayName => new ResourceLocString(nameof(Resources.RaymanDictées_Win32_Title));
    public override DateTime ReleaseDate => new(1998, 01, 01); // Not exact

    public override GameIconAsset Icon => GameIconAsset.RaymanDictées;
    public override GameBannerAsset Banner => GameBannerAsset.RaymanDictées;

    #endregion

    #region Protected Methods

    protected override void RegisterComponents(IGameComponentBuilder builder)
    {
        base.RegisterComponents(builder);

        builder.Register(new DiscordRichPresenceComponent("Rayman Dictées", "rayman_dictees"));
    }

    protected override ProgramInstallationStructure CreateStructure() => new DirectoryProgramInstallationStructure(new ProgramFileSystem(new ProgramPath[]
    {
        // Files
        new ProgramFilePath("Dictee.exe", ProgramPathType.PrimaryExe, required: true),
    }));

    #endregion

    #region Public Methods

    public override IEnumerable<GameAddAction> GetAddActions() => new GameAddAction[]
    {
        new LocateDirectoryGameAddAction(this),
    };

    #endregion
}