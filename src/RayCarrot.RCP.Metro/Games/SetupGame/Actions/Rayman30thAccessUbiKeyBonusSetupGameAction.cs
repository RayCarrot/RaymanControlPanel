namespace RayCarrot.RCP.Metro.Games.SetupGame;

public class Rayman30thAccessUbiKeyBonusSetupGameAction : InstallModSetupGameAction
{
    protected override long GameBananaModId => 668617;
    protected override string[] ModIds => ["Rayman30th.Gbc.UbiKey"];

    public override LocalizedString Header => new ResourceLocString(nameof(Resources.SetupGameAction_AccessUbiKeyBonus_Header));
    public override LocalizedString Info => new ResourceLocString(nameof(Resources.SetupGameAction_AccessUbiKeyBonus_Info));

    public override SetupGameActionType Type => SetupGameActionType.Recommended;
}