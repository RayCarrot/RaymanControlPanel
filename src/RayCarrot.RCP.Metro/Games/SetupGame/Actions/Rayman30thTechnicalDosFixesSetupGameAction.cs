namespace RayCarrot.RCP.Metro.Games.SetupGame;

public class Rayman30thTechnicalDosFixesSetupGameAction : InstallModSetupGameAction
{
    protected override long GameBananaModId => 668592;
    protected override string[] ModIds => ["Rayman30th.Dos.TechnicalFixes", "Rayman30th.Dos.OriginalPS1MusicAndTechnicalFixes"];

    public override LocalizedString Header => new ResourceLocString(nameof(Resources.SetupGameAction_TechnicalDosFixes_Header));
    public override LocalizedString Info => new ResourceLocString(nameof(Resources.SetupGameAction_TechnicalDosFixes_Info));

    public override SetupGameActionType Type => SetupGameActionType.Recommended;
}