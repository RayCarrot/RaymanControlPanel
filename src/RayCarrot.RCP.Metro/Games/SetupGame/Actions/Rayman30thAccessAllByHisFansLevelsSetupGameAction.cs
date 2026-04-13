namespace RayCarrot.RCP.Metro.Games.SetupGame;

public class Rayman30thAccessAllByHisFansLevelsSetupGameAction : InstallModSetupGameAction
{
    protected override long GameBananaModId => 668603;
    protected override string[] ModIds => ["Rayman30th.DosFan.AllLevels.Steam", "Rayman30th.DosFan.AllLevels.Uplay"];

    public override LocalizedString Header => new ResourceLocString(nameof(Resources.SetupGameAction_AccessAllByHisFansLevels_Header));
    public override LocalizedString Info => new ResourceLocString(nameof(Resources.SetupGameAction_AccessAllByHisFansLevels_Info));

    public override SetupGameActionType Type => SetupGameActionType.Optional;
}