using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace RayCarrot.RCP.Metro.Games.RichPresence;

public class GameRichPresenceManager_Rayman2_Win32 : GameRichPresenceManager
{
    #region Constructor

    public GameRichPresenceManager_Rayman2_Win32(GameInstallation gameInstallation, Process process) : base(gameInstallation, process) { }

    #endregion

    #region Private Properties

    // Names from raymap
    private Dictionary<string, string> LevelDisplayNames { get; } = new()
    {
        ["Menu"] = "Main Menu",
        ["Jail_10"] = "Prologue (Stormy Seas)",
        ["Jail_20"] = "Prologue (Jail)",
        ["Learn_10"] = "The Woods of Light",
        ["Mapmonde"] = "The Hall of Doors",
        ["Learn_30"] = "The Fairy Glade 1",
        ["Learn_31"] = "The Fairy Glade 2",
        ["Bast_20"] = "The Fairy Glade 3",
        ["Bast_22"] = "The Fairy Glade 4",
        ["Learn_60"] = "The Fairy Glade 5",
        ["Ski_10"] = "The Marshes of Awakening 1",
        ["Ski_60"] = "The Marshes of Awakening 2",
        ["Batam_10"] = "Meanwhile, on the Prison Ship",
        ["Chase_10"] = "The Bayou 1",
        ["Chase_22"] = "The Bayou 2",
        ["Ly_10"] = "The Walk of Life",
        ["nego_10"] = "The Chamber of the Teensies",
        ["Water_10"] = "The Sanctuary of Water and Ice 1",
        ["Water_20"] = "The Sanctuary of Water and Ice 2",
        ["poloc_10"] = "Polokus - First Mask",
        ["Rodeo_10"] = "The Menhir Hills 1",
        ["Rodeo_40"] = "The Menhir Hills 2",
        ["Rodeo_60"] = "The Menhir Hills 3",
        ["Vulca_10"] = "The Cave of Bad Dreams 1",
        ["Vulca_20"] = "The Cave of Bad Dreams 2",
        ["GLob_30"] = "The Canopy 1",
        ["GLob_10"] = "The Canopy 2",
        ["GLob_20"] = "The Canopy 3",
        ["Whale_00"] = "Whale Bay 1",
        ["Whale_05"] = "Whale Bay 2",
        ["Whale_10"] = "Whale Bay 3",
        ["Plum_00"] = "The Sanctuary of Stone and Fire 1",
        ["Plum_20"] = "The Sanctuary of Stone and Fire 2",
        ["Plum_10"] = "The Sanctuary of Stone and Fire 3",
        ["poloc_20"] = "Polokus - Second Mask",
        ["Bast_09"] = "The Echoing Caves (Intro)",
        ["Bast_10"] = "The Echoing Caves 1",
        ["Cask_10"] = "The Echoing Caves 2",
        ["Cask_30"] = "The Echoing Caves 3",
        ["Nave_10"] = "The Precipice 1",
        ["Nave_15"] = "The Precipice 2",
        ["Nave_20"] = "The Precipice 3",
        ["Seat_10"] = "The Top of the World 1",
        ["Seat_11"] = "The Top of the World 2",
        ["Earth_10"] = "The Sanctuary of Rock and Lava 1",
        ["Earth_20"] = "The Sanctuary of Rock and Lava 2",
        ["Earth_30"] = "The Sanctuary of Rock and Lava 3",
        ["Ly_20"] = "The Walk of Power",
        ["Helic_10"] = "Beneath the Sanctuary of Rock and Lava 1",
        ["Helic_20"] = "Beneath the Sanctuary of Rock and Lava 2",
        ["Helic_30"] = "Beneath the Sanctuary of Rock and Lava 3",
        ["poloc_30"] = "Polokus - Third Mask",
        ["Morb_00"] = "Tomb of the Ancients 1",
        ["Morb_10"] = "Tomb of the Ancients 2",
        ["Morb_20"] = "Tomb of the Ancients 3",
        ["Learn_40"] = "The Iron Mountains 1",
        ["Ball"] = "The Iron Mountains (Balloon Flight)",
        ["Ile_10"] = "The Iron Mountains 2 (The Gloomy Island)",
        ["Mine_10"] = "The Iron Mountains 3 (The Pirate Mines)",
        ["poloc_40"] = "Polokus - Fourth Mask",
        ["Batam_20"] = "Meanwhile, on the Prison Ship (The Grolgoth)",
        ["Boat01"] = "The Prison Ship 1",
        ["Boat02"] = "The Prison Ship 2",
        ["Astro_00"] = "The Prison Ship 3",
        ["Astro_10"] = "The Prison Ship 4",
        ["Rhop_10"] = "The Crow's Nest",
        ["End_10"] = "Ending",
        ["Staff_10"] = "Staff Roll",
        ["Bonux"] = "Bonus Level",
        ["Raycap"] = "Score Recap"
    };

    #endregion

    #region Public Methods

    public override unsafe string? GetPresence()
    {
        // Read the EngineStructure instance
        GAM_tdstEngineStructure engineStructure = Reader.Read<GAM_tdstEngineStructure>(Reader.BaseAddress + 0x100380);

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