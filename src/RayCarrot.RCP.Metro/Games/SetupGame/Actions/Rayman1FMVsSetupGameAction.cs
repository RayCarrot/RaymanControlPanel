using RayCarrot.RCP.Metro.Games.Tools.PerLevelSoundtrack;

namespace RayCarrot.RCP.Metro.Games.SetupGame;

public class Rayman1FMVsSetupGameAction : InstallModSetupGameAction
{
    protected override long GameBananaModId => 636650;
    protected override string[] ModIds => ["Rayman1.GameMod.FMVs"];

    // TODO-LOC
    public override LocalizedString Header => "Install the Rayman FMVs mod";
    public override LocalizedString Info => "Some versions of the game don't come with the intro and outro FMVs. By using the Rayman FMVs mod, alongside the per-level soundtrack tool, it allows the FMVs to be restored.";

    public override SetupGameActionType Type => SetupGameActionType.Recommended;

    public override bool CheckIsAvailable(GameInstallation gameInstallation)
    {
        return gameInstallation.GetObject<PerLevelSoundtrackData>(GameDataKey.R1_PerLevelSoundtrackData) is { IsEnabled: true };
    }

    public override bool CheckIsComplete(GameInstallation gameInstallation)
    {
        // Override checking if the mod is installed to instead check if the intro file exist
        return (gameInstallation.InstallLocation.Directory + "INTRO.DAT").FileExists;
    }

}