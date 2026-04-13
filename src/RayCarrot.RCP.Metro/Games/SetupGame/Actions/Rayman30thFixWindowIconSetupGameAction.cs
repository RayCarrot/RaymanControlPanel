namespace RayCarrot.RCP.Metro.Games.SetupGame;

public class Rayman30thFixWindowIconSetupGameAction : InstallModSetupGameAction
{
    protected override long GameBananaModId => 669248;
    protected override string[] ModIds => ["Rayman30th.Fix.WindowIcon.Steam", "Rayman30th.Fix.WindowIcon.Uplay"];

    public override LocalizedString Header => new ResourceLocString(nameof(Resources.SetupGameAction_FixWindowIcon_Header));
    public override LocalizedString Info => new ResourceLocString(nameof(Resources.SetupGameAction_FixWindowIcon_Info));

    public override SetupGameActionType Type => SetupGameActionType.Recommended;
}