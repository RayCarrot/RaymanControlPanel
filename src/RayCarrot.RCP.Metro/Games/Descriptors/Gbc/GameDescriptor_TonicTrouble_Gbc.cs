using RayCarrot.RCP.Metro.Games.Components;
using RayCarrot.RCP.Metro.Games.SetupGame;
using RayCarrot.RCP.Metro.Games.Structure;

namespace RayCarrot.RCP.Metro;

/// <summary>
/// The Tonic Trouble (GBC) game descriptor
/// </summary>
public sealed class GameDescriptor_TonicTrouble_Gbc : GbcGameDescriptor
{
    #region Public Properties

    public override string GameId => "TonicTrouble_Gbc";
    public override Game Game => Game.TonicTrouble_Gbc;
    public override GameCategory Category => GameCategory.Other;

    public override LocalizedString DisplayName => new ResourceLocString(nameof(Resources.TonicTrouble_Gbc_Title));
    public override string[] SearchKeywords => new[] { "tt", "gbc" };
    public override DateTime ReleaseDate => new(2000, 01, 01); // Not exact

    public override GameIconAsset Icon => GameIconAsset.TonicTrouble_Gbc;
    public override GameBannerAsset Banner => GameBannerAsset.TonicTrouble;

    #endregion

    #region Protected Methods

    protected override void RegisterComponents(IGameComponentBuilder builder)
    {
        base.RegisterComponents(builder);

        builder.Register(new SetupGameActionComponent(_ => new TonicTroubleGBCRemoveUbiKeyRequirementSetupGameAction()));

        builder.Register(new GameBananaGameComponent(24154));
    }

    protected override ProgramInstallationStructure CreateStructure() => new GbcRomProgramInstallationStructure(new[]
    {
        new GbcProgramLayout("EU", "TONICTROUBL", "AXTP", "41"),
    });

    #endregion

    #region Public Methods

    public override IEnumerable<GameAddAction> GetAddActions() => new GameAddAction[]
    {
        new LocateFileGameAddAction(this),
    };

    #endregion
}