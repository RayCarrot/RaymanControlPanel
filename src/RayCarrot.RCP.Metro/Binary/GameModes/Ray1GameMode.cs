using BinarySerializer.Ray1;

namespace RayCarrot.RCP.Metro;

// TODO: Localize game mode names?
public enum Ray1GameMode
{
    [Ray1GameModeInfo("Rayman Prototype (SNES)", Ray1EngineVersion.SNES)]
    Rayman1_SnesProto,

    [Ray1GameModeInfo("Rayman Prototype (Jaguar)", Ray1EngineVersion.Jaguar_Proto)]
    Rayman1_JaguarProto,

    [Ray1GameModeInfo("Rayman Demo (Jaguar)", Ray1EngineVersion.Jaguar_Demo)]
    Rayman1_JaguarDemo,

    [Ray1GameModeInfo("Rayman (Jaguar)", Ray1EngineVersion.Jaguar)]
    Rayman1_Jaguar,

    [Ray1GameModeInfo("Rayman (PlayStation)", Ray1EngineVersion.PS1)]
    Rayman1_PS1,

    [Ray1GameModeInfo("Rayman (PC)", Ray1EngineVersion.PC)]
    Rayman1_PC,

    [Ray1GameModeInfo("Rayman Edutainment (PC)", Ray1EngineVersion.PC_Edu)]
    RaymanEducational_PC,

    [Ray1GameModeInfo("Rayman Edutainment (PlayStation)", Ray1EngineVersion.PS1_Edu)]
    RaymanEducational_PS1,

    [Ray1GameModeInfo("Rayman Designer (PC)", Ray1EngineVersion.PC_Kit)]
    RaymanDesigner_PC,

    [Ray1GameModeInfo("Rayman by his Fans (PC)", Ray1EngineVersion.PC_Fan)]
    RaymanByHisFans_PC,

    [Ray1GameModeInfo("Rayman 60 Levels (PC)", Ray1EngineVersion.PC_Fan)]
    Rayman60Levels_PC,

    [Ray1GameModeInfo("Rayman Advance (Game Boy Advance)", Ray1EngineVersion.GBA)]
    Rayman1_GBA,

    [Ray1GameModeInfo("Rayman 2 (PS1 - Prototype)", Ray1EngineVersion.R2_PS1)]
    Rayman2_PS1,
}