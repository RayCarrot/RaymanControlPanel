namespace RayCarrot.RCP.Metro.Games.SetupGame;

public class Rayman30thAccessUbiKeyBonusSetupGameAction : InstallModSetupGameAction
{
    protected override long GameBananaModId => 668617;
    protected override string[] ModIds => ["Rayman30th.Gbc.UbiKey"];

    // TODO-LOC
    public override LocalizedString Header => "Install the Access Ubi Key Bonus (GBC) mod";
    public override LocalizedString Info => "There is a bonus level which can normally only be unlocked by linking the game to another supported title through the Ubi Key feature. This feature is however not available in the collection and there is no other way to access the level. The Access Ubi Key Bonus (GBC) mod by RayCarrot removes this requirement, allowing the level to be accessed.";

    public override SetupGameActionType Type => SetupGameActionType.Recommended;
}