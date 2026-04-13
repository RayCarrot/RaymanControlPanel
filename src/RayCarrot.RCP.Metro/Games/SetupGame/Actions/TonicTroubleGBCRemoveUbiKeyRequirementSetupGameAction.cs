namespace RayCarrot.RCP.Metro.Games.SetupGame;

public class TonicTroubleGBCRemoveUbiKeyRequirementSetupGameAction : InstallModSetupGameAction
{
    protected override long GameBananaModId => 647849;
    protected override string[] ModIds => ["TonicTroubleGbc.GameMod.UbiKey"];

    public override LocalizedString Header => new ResourceLocString(nameof(Resources.SetupGameAction_GBCRemoveUbiKeyRequirement_Header));
    public override LocalizedString Info => new ResourceLocString(nameof(Resources.SetupGameAction_GBCRemoveUbiKeyRequirement_Info));

    public override SetupGameActionType Type => SetupGameActionType.Recommended;
}