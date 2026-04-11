namespace RayCarrot.RCP.Metro.Games.SetupGame;

public class Rayman30thAccessAllByHisFansLevelsSetupGameAction : InstallModSetupGameAction
{
    protected override long GameBananaModId => 668603;
    protected override string[] ModIds => ["Rayman30th.DosFan.AllLevels"];

    // TODO-LOC
    public override LocalizedString Header => "Install the Access all Levels (Rayman by his Fans) mod";
    public override LocalizedString Info => "By default there are two levels (\"The enchanted forest\" and \"High flyer\") which were made inaccessible. This is due to the levels having a bug where using a checkpoint twice causes the game to crash. The Access all Levels (Rayman by his Fans) mod by RayCarrot restores access to these levels.";

    public override SetupGameActionType Type => SetupGameActionType.Optional;
}