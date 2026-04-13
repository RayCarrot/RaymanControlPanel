namespace RayCarrot.RCP.Metro.Games.SetupGame;

public class Rayman30thFixWindowIconSetupGameAction : InstallModSetupGameAction
{
    protected override long GameBananaModId => 669248;
    protected override string[] ModIds => ["Rayman30th.Fix.WindowIcon.Steam", "Rayman30th.Fix.WindowIcon.Uplay"];

    // TODO-LOC
    public override LocalizedString Header => "Install the Fix Window Icon mod";
    public override LocalizedString Info => "Due to the game trying to load the icon from the wrong resource it will not display in the taskbar when the game is running. The Fix Window Icon mod by RayCarrot fixes this issue.";

    public override SetupGameActionType Type => SetupGameActionType.Recommended;
}