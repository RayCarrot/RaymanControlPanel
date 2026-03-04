using RayCarrot.RCP.Metro.Games.Components;
using RayCarrot.RCP.Metro.Games.SetupGame;
using RayCarrot.RCP.Metro.Games.Structure;

namespace RayCarrot.RCP.Metro;

/// <summary>
/// The Rayman 1 (GBC) game descriptor
/// </summary>
public sealed class GameDescriptor_Rayman1_Gbc : GbcGameDescriptor
{
    #region Public Properties

    public override string GameId => "Rayman1_Gbc";
    public override Game Game => Game.Rayman1_Gbc;
    public override GameCategory Category => GameCategory.Handheld;

    public override LocalizedString DisplayName => new ResourceLocString(nameof(Resources.Rayman1_Gbc_Title));
    public override string[] SearchKeywords => new[] { "r1", "gbc" };
    public override DateTime ReleaseDate => new(2000, 02, 01);

    public override GameIconAsset Icon => GameIconAsset.Rayman1_Gbc;
    public override GameBannerAsset Banner => GameBannerAsset.Rayman1_Gbc;

    #endregion

    #region Protected Methods

    protected override void RegisterComponents(IGameComponentBuilder builder)
    {
        base.RegisterComponents(builder);

        builder.Register(new RayMapComponent(RayMapComponent.RayMapViewer.Ray1Map, "RaymanGBC", "gbc/r1_eu"));

        builder.Register(new SetupGameActionComponent(_ => new Rayman1GBCRemoveUbiKeyRequirementSetupGameAction()));

        builder.Register(new GameBananaGameComponent(23858));
    }

    protected override ProgramInstallationStructure CreateStructure() => new GbcRomProgramInstallationStructure(new[]
    {
        new GbcProgramLayout("EU", "RAYMAN", "AYQP", "41"),
        new GbcProgramLayout("US", "RAYMAN", "AYQE", "41"),
        new GbcProgramLayout("JP", "RAYMAN", "BURJ", "2H"),
    });

    #endregion

    #region Public Methods

    public override IEnumerable<GameAddAction> GetAddActions() => new GameAddAction[]
    {
        new LocateFileGameAddAction(this),
    };

    #endregion
}