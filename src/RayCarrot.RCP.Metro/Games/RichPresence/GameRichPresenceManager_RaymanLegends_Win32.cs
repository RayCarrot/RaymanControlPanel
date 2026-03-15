using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace RayCarrot.RCP.Metro.Games.RichPresence;

// TODO-UPDATE: Support Ubisoft Connect version (validate version by checking for some constant value in memory?)
public class GameRichPresenceManager_RaymanLegends_Win32 : GameRichPresenceManager
{
    #region Constructor

    public GameRichPresenceManager_RaymanLegends_Win32(GameInstallation gameInstallation, Process process) : base(gameInstallation, process) { }

    #endregion

    #region Private Methods

    private static string RemoveCommandFromString(string str)
    {
        char[] newStr = new char[str.Length];
        int newStrLength = 0;

        bool insideCmd = false;
        foreach (char c in str)
        {
            if (insideCmd)
            {
                if (c == ']')
                    insideCmd = false;
            }
            else if (c == '[')
            {
                insideCmd = true;
            }
            else
            {
                newStr[newStrLength] = c;
                newStrLength++;
            }
        }

        return new string(newStr, 0, newStrLength);
    }

    private string? GetLocalizedText(uint locId)
    {
        // Read the pointer to the singleton LocalisationManager instance
        long localisationManagerPtr = Reader.ReadPointer(Reader.BaseAddress + 0xa461d0);

        // Get the current language (text for other languages is sadly not loaded)
        int language = Reader.Read<int>(localisationManagerPtr);

        // Find the map for the specified language
        if (FindInMap<int, Map>(Reader.Read<Map>(localisationManagerPtr + 4), language, out Map locTextMap))
        {
            // Find the LocText for the specified ID
            if (FindInMap<uint, ITFString>(locTextMap, locId, out ITFString locText))
            {
                // Read the UTF-8 string
                return Reader.ReadString(locText.StringPtr, Encoding.UTF8, locText.Length);
            }
        }

        return null;
    }

    private bool FindInMap<TKey, TValue>(Map map, TKey key, out TValue value)
        where TKey : unmanaged
        where TValue : unmanaged
    {
        MapNode<TKey, TValue>? nextNode = Reader.ReadNullable<MapNode<TKey, TValue>>(map.RootNodePtr);
        MapNode<TKey, TValue>? currentNode = null;

        while (nextNode is { } next)
        {
            if (Comparer<TKey>.Default.Compare(next.Key, key) < 0)
            {
                nextNode = Reader.ReadNullable<MapNode<TKey, TValue>>(next.RightPointer);
            }
            else
            {
                currentNode = nextNode;
                nextNode = Reader.ReadNullable<MapNode<TKey, TValue>>(next.LeftPointer);
            }
        }

        if (currentNode is { } result && Comparer<TKey>.Default.Compare(result.Key, key) <= 0)
        {
            value = result.Value;
            return true;
        }
        else
        {
            value = default;
            return false;
        }
    }

    private string? GetCurrentLevelName(long gameManagerPtr)
    {
        // Read the GameDataManager instance
        long gameDataManagerPtr = Reader.ReadPointer(Reader.BaseAddress + 0xaeb750);

        // Read the current level tag from the game data
        uint currentLevelTag = Reader.Read<uint>(gameDataManagerPtr + 0x8);

        // Read the pointer to the RO2_GameManagerConfig_Template
        long gameManagerConfigTemplatePtr = Reader.ReadPointer(gameManagerPtr + 0x274);

        // Find the tag text loc ID in the map
        if (!FindInMap<uint, uint>(Reader.Read<Map>(gameManagerConfigTemplatePtr + 0x15FC), currentLevelTag, out uint locId)) 
            return null;

        // Get the level name from the localisation
        string? levelName = GetLocalizedText(locId);

        if (levelName == null) 
            return null;
        
        levelName = RemoveCommandFromString(levelName);
        return levelName;
    }

    #endregion

    #region Public Methods

    public override string? GetPresence()
    {
        // Read the GameManager instance
        long gameManagerPtr = Reader.ReadPointer(Reader.BaseAddress + 0xae483c);

        // Make sure it's not null (might happen during startup)
        if (gameManagerPtr == 0)
            return null;

        // Read the current GameScreen instance ID
        GameScreenId currentGameScreenId = Reader.Read<GameScreenId>(gameManagerPtr + 0x8);

        switch (currentGameScreenId)
        {
            case GameScreenId.RO2_GS_AdversarialSoccer:
                return "Kung Foot";

            case GameScreenId.RO2_GS_ChallengeEndurance:
                return "Challenges";

            case GameScreenId.RO2_GS_ChallengeTraining:
                return "Training";

            case GameScreenId.RO2_GS_DuckMode:
            case GameScreenId.RO2_GS_Gameplay:
            case GameScreenId.RO2_GS_Invasion:
                return GetCurrentLevelName(gameManagerPtr);

            case GameScreenId.RO2_GS_Home:
                return "Gallery";

            case GameScreenId.RO2_GS_MainMenu:
                return "Main Menu";

            case GameScreenId.RO2_GS_AdversarialGoldenPotato: // Unused
            case GameScreenId.RO2_GS_Init:
            case GameScreenId.RO2_GS_InteractiveLoading:
            case GameScreenId.RO2_GS_Loading:
            case GameScreenId.RO2_GS_LoadingMovie:
            default:
                return null;
        }
    }

    #endregion

    #region Data Types

    public enum GameScreenId : uint
    {
        RO2_GS_AdversarialGoldenPotato = 0x46B080AC,
        RO2_GS_AdversarialSoccer = 0x002B90F4,
        RO2_GS_ChallengeEndurance = 0x93E13F56,
        RO2_GS_ChallengeTraining = 0xEE9FBE5C,
        RO2_GS_DuckMode = 0xF34C1E7B,
        RO2_GS_Gameplay = 0xB360CC34,
        RO2_GS_Home = 0x130996E6,
        RO2_GS_Init = 0xCFA8C0CA,
        RO2_GS_InteractiveLoading = 0xB2DD2019,
        RO2_GS_Invasion = 0x04D51AAA,
        RO2_GS_Loading = 0x666D02BE,
        RO2_GS_LoadingMovie = 0xD7EE15BB,
        RO2_GS_MainMenu = 0xAD9F1645,
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct ITFString
    {
        public uint StringPtr;
        public uint Uint_04;
        public int Length;
        public uint Uint_0C;
        public uint Uint_10;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct Map
    {
        public uint Uint_00;
        public uint Uint_04;
        public uint RootNodePtr;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MapNode<TKey, TValue>
        where TKey : unmanaged
        where TValue : unmanaged
    {
        public uint LeftPointer;
        public uint RightPointer;
        public uint ParentPointer;
        public uint Uint_0C;
        public TKey Key;
        public TValue Value;
    }

    #endregion
}