namespace RayCarrot.RCP.Metro.Games.SetupGame;

public class Rayman1GBCRemoveUbiKeyRequirementSetupGameAction : InstallModSetupGameAction
{
    protected override long GameBananaModId => 636637;
    protected override string[] ModIds => ["RaymanGbc.GameMod.UbiKey"];

    // TODO-LOC
    public override LocalizedString Header => "Install the Remove Ubi Key Requirement mod";
    public override LocalizedString Info => "There is a bonus level which can normally only be unlocked by linking the game to another supported title through the Ubi Key feature. The Remove Ubi Key Requirement mod by RayCarrot removes this requirements, allowing the level to be accessed without linking to another game.";

    public override SetupGameActionType Type => SetupGameActionType.Optional;
}