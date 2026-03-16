using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace RayCarrot.RCP.Metro.Games.RichPresence;

public class GameRichPresenceManager_Rayman3_Win32 : GameRichPresenceManager
{
    #region Constructor

    public GameRichPresenceManager_Rayman3_Win32(GameInstallation gameInstallation, Process process) : base(gameInstallation, process) { }

    #endregion

    #region Constant Fields

    private const long EngineStructureAddress = 0x3D7DC0;

    #endregion

    #region Private Properties

    // Names from raymap
    private Dictionary<string, string> LevelDisplayNames { get; } = new(StringComparer.OrdinalIgnoreCase)
    {
        ["menumap"] = "Main Menu",
        ["intro_10"] = "The Fairy Council 1 (Murfy)",
        ["intro_15"] = "The Fairy Council 2 (Finding Globox)",
        ["intro_17"] = "The Fairy Council 3 (Inside)",
        ["intro_20"] = "The Fairy Council 4",
        ["menu_00"] = "The Fairy Council 5 (Heart of the World)",
        ["sk8_00"] = "The Fairy Council 6 (Teensie Highway)",
        ["wood_11"] = "Clearleaf Forest 1",
        ["wood_10"] = "Clearleaf Forest 2",
        ["wood_19"] = "Clearleaf Forest 3",
        ["wood_50"] = "Clearleaf Forest 4 (Master Kaag)",
        ["menu_10"] = "Clearleaf Forest 5 (Doctor's Office)",
        ["sk8_10"] = "Clearleaf Forest 6 (Teensie Highway)",
        ["swamp_60"] = "The Bog of Murk 1 (Bégoniax)",
        ["swamp_82"] = "The Bog of Murk 2",
        ["swamp_81"] = "The Bog of Murk 3",
        ["swamp_83"] = "The Bog of Murk 4",
        ["swamp_50"] = "The Bog of Murk 5 (Razoff's Mansion)",
        ["swamp_51"] = "The Bog of Murk 6 (Razoff's Basement)",
        ["moor_00"] = "The Land of the Livid Dead 1",
        ["moor_30"] = "The Land of the Livid Dead 2",
        ["moor_60"] = "The Land of the Livid Dead 3 (Tower)",
        ["moor_19"] = "The Land of the Livid Dead 4 (Céloche)",
        ["menu_20"] = "The Land of the Livid Dead 5 (Doctor's Office)",
        ["sk8_20"] = "The Land of the Livid Dead 6 (Teensie Highway)",
        ["knaar_10"] = "The Desert of the Knaaren 1",
        ["knaar_20"] = "The Desert of the Knaaren 2 (The Great Hall)",
        ["knaar_30"] = "The Desert of the Knaaren 3 (Tower)",
        ["knaar_45"] = "The Desert of the Knaaren 4",
        ["knaar_60"] = "The Desert of the Knaaren 5 (Arena)",
        ["knaar_69"] = "The Desert of the Knaaren 6 (Grimace Room)",
        ["knaar_70"] = "The Desert of the Knaaren 7",
        ["menu_30"] = "The Desert of the Knaaren 8 (Doctor's Office)",
        ["flash_20"] = "The Longest Shortcut 1",
        ["flash_30"] = "The Longest Shortcut 2",
        ["flash_10"] = "The Longest Shortcut 3",
        ["sea_10"] = "The Summit Beyond the Clouds 1 (The Looming Sea)",
        ["mount_50"] = "The Summit Beyond the Clouds 2",
        ["mount_4x"] = "The Summit Beyond the Clouds 3 (Snowboard)",
        ["fact_40"] = "Hoodlum Headquarters 1",
        ["fact_50"] = "Hoodlum Headquarters 2 (Firing Range)",
        ["fact_55"] = "Hoodlum Headquarters 3",
        ["fact_34"] = "Hoodlum Headquarters 4 (Horrible Machine)",
        ["fact_22"] = "Hoodlum Headquarters 5 (Rising Lava)",
        ["tower_10"] = "The Tower of the Leptys 1",
        ["tower_20"] = "The Tower of the Leptys 2",
        ["tower_30"] = "The Tower of the Leptys 3",
        ["tower_40"] = "The Tower of the Leptys 4",
        ["lept_15"] = "The Tower of the Leptys 5 (Final Battle)",
        ["staff"] = "Staff Roll",
        ["toudi_00"] = "Arcade - 2D Madness",
        ["ten_map"] = "Arcade - Racket Jump",
        ["crush"] = "Arcade - Crush",
        ["raz_map"] = "Arcade - Razoff Circus",
        ["sentinel"] = "Arcade - Sentinel",
        ["snipe_00"] = "Arcade - Missile Command",
        ["ballmap"] = "Arcade - Balloons",
        ["ship_map"] = "Arcade - Special Invaders",
        ["commando"] = "Arcade - Commando",
        ["BonusTXT"] = "Bonus",
        ["endgame"] = "Endgame"
    };

    #endregion

    #region Public Methods

    public override unsafe string? GetPresence()
    {
        // Read the EngineStructure instance
        GAM_tdstEngineStructure engineStructure = Reader.Read<GAM_tdstEngineStructure>(Reader.BaseAddress + EngineStructureAddress);

        // Get the current level name
        string levelName = Encoding.ASCII.GetString(engineStructure.szLevelName, MAX_NAME_LEVEL);

        // Trim null
        levelName = levelName.TrimEnd('\0');

        // Get the display name for the level
        if (LevelDisplayNames.TryGetValue(levelName, out string displayName))
            return displayName;
        else
            return null;
    }

    #endregion

    #region Data Types

    private const int MAX_NAME_LEVEL = 30;

    [StructLayout(LayoutKind.Sequential)]
    private unsafe struct GAM_tdstEngineStructure
    {
        public byte eEngineMode;
        public fixed byte szLevelName[MAX_NAME_LEVEL];
        // NOTE: No need to define the remaining data since we don't use it
    }

    #endregion
}