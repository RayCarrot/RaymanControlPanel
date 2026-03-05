namespace RayCarrot.RCP.Metro.Games.SetupGame;

public class Rayman1GBCRemoveUbiKeyRequirementSetupGameAction : InstallModSetupGameAction
{
    protected override long GameBananaModId => 636637;
    protected override string[] ModIds => ["RaymanGbc.GameMod.UbiKey"];

    public override LocalizedString Header => new ResourceLocString(nameof(Resources.SetupGameAction_GBCRemoveUbiKeyRequirement_Header));
    public override LocalizedString Info => new ResourceLocString(nameof(Resources.SetupGameAction_GBCRemoveUbiKeyRequirement_Info));

    public override SetupGameActionType Type => SetupGameActionType.Optional;
}