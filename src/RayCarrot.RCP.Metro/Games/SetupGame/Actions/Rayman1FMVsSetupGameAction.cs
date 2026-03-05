using RayCarrot.RCP.Metro.Games.Tools.PerLevelSoundtrack;

namespace RayCarrot.RCP.Metro.Games.SetupGame;

public class Rayman1FMVsSetupGameAction : InstallModSetupGameAction
{
    protected override long GameBananaModId => 636650;
    protected override string[] ModIds => ["Rayman1.GameMod.FMVs"];

    public override LocalizedString Header => new ResourceLocString(nameof(Resources.SetupGameAction_Rayman1FMVs_Header));
    public override LocalizedString Info => new ResourceLocString(nameof(Resources.SetupGameAction_Rayman1FMVs_Info));

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