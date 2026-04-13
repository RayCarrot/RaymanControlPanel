using RayCarrot.RCP.Metro.Games.Components;
using RayCarrot.RCP.Metro.Games.Structure;

namespace RayCarrot.RCP.Metro;

/// <summary>
/// The Rayman 1 Prototype (SNES) game descriptor
/// </summary>
public sealed class GameDescriptor_Rayman1_Prototype_Snes : SnesGameDescriptor
{
    #region Public Properties

    public override string GameId => "Rayman1_Prototype_Snes";
    public override Game Game => Game.Rayman1;
    public override GameCategory Category => GameCategory.Rayman;
    public override GameType Type => GameType.Prototype;

    public override LocalizedString DisplayName => new ResourceLocString(nameof(Resources.Rayman1_Prototype_Snes_Title));
    public override DateTime ReleaseDate => new(1992, 01, 01); // Unknown

    public override GameIconAsset Icon => GameIconAsset.Rayman1_Snes;
    public override GameBannerAsset Banner => GameBannerAsset.Rayman1_Snes;

    #endregion

    #region Protected Methods

    protected override void RegisterComponents(IGameComponentBuilder builder)
    {
        base.RegisterComponents(builder);

        builder.Register(new RayMapComponent(RayMapComponent.RayMapViewer.Ray1Map, "RaymanSNES", "snes/proto"));
        builder.Register<BinaryGameModeComponent>(new Ray1GameModeComponent(Ray1GameMode.Rayman1_SnesProto));
    }

    protected override ProgramInstallationStructure CreateStructure() => new SnesRomProgramInstallationStructure();

    #endregion

    #region Public Methods

    public override IEnumerable<GameAddAction> GetAddActions() => new GameAddAction[]
    {
        new LocateFileGameAddAction(this),
    };

    #endregion
}