using RayCarrot.RCP.Metro.Games.Components;
using RayCarrot.RCP.Metro.Games.Structure;

namespace RayCarrot.RCP.Metro;

/// <summary>
/// The Rayman 2 Beta 1998/07/22 (Win32) game descriptor
/// </summary>
public sealed class GameDescriptor_Rayman2_Beta_19980722_Win32 : Win32GameDescriptor
{
    #region Public Properties

    public override string GameId => "Rayman2_Beta_19980722_Win32";
    public override Game Game => Game.Rayman2;
    public override GameCategory Category => GameCategory.Rayman;
    public override GameType Type => GameType.Prototype;

    public override LocalizedString DisplayName => new ResourceLocString(nameof(Resources.Rayman2_Beta_19980722_Win32_Title));
    public override DateTime ReleaseDate => new(1998, 07, 22);

    public override GameIconAsset Icon => GameIconAsset.Rayman2_Demo;
    public override GameBannerAsset Banner => GameBannerAsset.Rayman2;

    #endregion

    #region Protected Methods

    protected override void RegisterComponents(IGameComponentBuilder builder)
    {
        base.RegisterComponents(builder);

        builder.Register(new GameBananaGameComponent(21335));

        builder.Register(new DiscordRichPresenceComponent("Rayman 2 Beta", "rayman_2"));
    }

    protected override ProgramInstallationStructure CreateStructure() => new DirectoryProgramInstallationStructure(new ProgramFileSystem(new ProgramPath[]
    {
        // Files
        new ProgramFilePath("Start.exe", ProgramPathType.PrimaryExe, required: true),
        new ProgramFilePath("MaiCFXvr.exe", ProgramPathType.OtherExe, required: true),

        // Directories
        new ProgramDirectoryPath("BinData", ProgramPathType.Data, required: true)
        {
            new ProgramDirectoryPath("World", ProgramPathType.Data, required: true),
        },
    }));

    #endregion

    #region Public Methods

    public override IEnumerable<GameAddAction> GetAddActions() => new GameAddAction[]
    {
        new LocateDirectoryGameAddAction(this),
    };

    #endregion
}