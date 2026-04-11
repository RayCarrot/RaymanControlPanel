namespace RayCarrot.RCP.Metro.Games.SetupGame;

public class Rayman30thTechnicalDosFixesSetupGameAction : InstallModSetupGameAction
{
    protected override long GameBananaModId => 668592;
    protected override string[] ModIds => ["Rayman30th.Dos.TechnicalFixes", "Rayman30th.Dos.OriginalPS1MusicAndTechnicalFixes"];

    // TODO-LOC
    public override LocalizedString Header => "Install the Technical Fixes (DOS) mod";
    public override LocalizedString Info => "The MS-DOS versions have several issues in the collection, such as there being lag in some levels, parallax scrolling being disabled and the Mr Dark boss music not playing. The Technical Fixes (DOS) mod by RayCarrot fixes these issues.";

    public override SetupGameActionType Type => SetupGameActionType.Recommended;
}