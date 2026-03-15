using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace RayCarrot.RCP.Metro.Games.RichPresence;

// TODO-UPDATE: Support Ubisoft Connect version (validate version by checking for some constant value in memory?)
public class GameRichPresenceManager_RaymanOrigins_Win32 : GameRichPresenceManager
{
    #region Constructor

    public GameRichPresenceManager_RaymanOrigins_Win32(GameInstallation gameInstallation, Process process) : base(gameInstallation, process) { }

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
        long localisationManagerPtr = Reader.ReadPointer(Reader.BaseAddress + 0x68a9d0);

        // Get the current language (text for other languages is sadly not loaded)
        int language = Reader.Read<int>(localisationManagerPtr);

        // Find the map for the specified language
        if (FindInMap<int, Map>(Reader.Read<Map>(localisationManagerPtr + 4), language, out Map locTextMap))
        {
            // Find the LocText for the specified ID
            if (FindInMap<uint, ITFString>(locTextMap, locId, out ITFString locText))
            {
                // Read the unicode string
                return Reader.ReadString(locText.String16Ptr, Encoding.Unicode, locText.Length * 2);
            }
        }

        return null;
    }

    private bool FindInMap<TKey, TValue>(Map map, TKey key, out TValue value)
        where TKey : unmanaged
        where TValue : unmanaged
    {
        long mapHeaderPtr = map.HeaderNodePtr; // Sentinel node
        long currentNodePtr = mapHeaderPtr;
        MapNode<TKey, TValue> currentNode = Reader.Read<MapNode<TKey, TValue>>(mapHeaderPtr);

        long nextNodePtr = currentNode.ParentPointer;
        MapNode<TKey, TValue> nextNode = Reader.Read<MapNode<TKey, TValue>>(currentNode.ParentPointer);
        while (!nextNode.End)
        {
            if (Comparer<TKey>.Default.Compare(nextNode.Key, key) < 0)
            {
                nextNodePtr = nextNode.RightPointer;
                nextNode = Reader.Read<MapNode<TKey, TValue>>(nextNode.RightPointer);
            }
            else
            {
                currentNodePtr = nextNodePtr;
                currentNode = nextNode;
                nextNodePtr = nextNode.LeftPointer;
                nextNode = Reader.Read<MapNode<TKey, TValue>>(nextNode.LeftPointer);
            }
        }

        if (currentNodePtr != mapHeaderPtr && Comparer<TKey>.Default.Compare(currentNode.Key, key) <= 0)
        {
            value = currentNode.Value;
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
        // Read the pointer to the PersistentGameData_Universe
        long persistentGameDataUniversePtr = Reader.ReadPointer(gameManagerPtr + 0x148);

        // Read the current level tag from the worldmap data
        uint currentLevelTag = Reader.Read<uint>(persistentGameDataUniversePtr + 0xB8);

        // Read the pointer to the Ray_GameManagerConfig_Template
        long gameManagerConfigTemplatePtr = Reader.ReadPointer(gameManagerPtr + 0x2d4);

        // Find the MapConfig index in the map
        if (!FindInMap<uint, int>(Reader.Read<Map>(gameManagerConfigTemplatePtr + 0x520), currentLevelTag, out int mapConfigIndex)) 
            return null;
        
        // Read the pointer to the MapConfig array
        long mapConfigArrayPtr = Reader.ReadPointer(gameManagerConfigTemplatePtr + 0x504);

        // Get the title ID for the MapConfig
        uint titleId = Reader.Read<uint>(mapConfigArrayPtr + mapConfigIndex * 0xf4 + 0x98);

        // Get the level name from the localisation
        string? levelName = GetLocalizedText(titleId);

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
        long gameManagerPtr = Reader.ReadPointer(Reader.BaseAddress + 0x6faebc);

        // Make sure it's not null (might happen during startup)
        if (gameManagerPtr == 0)
            return null;

        // Read the current GameScreen instance ID
        GameScreenId currentGameScreenId = Reader.Read<GameScreenId>(gameManagerPtr + 0x10);

        switch (currentGameScreenId)
        {
            case GameScreenId.MainMenu:
                return "Main Menu";
            
            case GameScreenId.WorldMap:
                return "Worldmap";

            case GameScreenId.Gameplay:
                return GetCurrentLevelName(gameManagerPtr);

            case GameScreenId.CheckpointScore: // Not sure what this is
            case GameScreenId.LevelStats: // Seems unused?
            case GameScreenId.Initial:
            case GameScreenId.Framework:
            case GameScreenId.WorldMapLoading:
            default:
                return null;
        }
    }

    #endregion

    #region Data Types

    public enum GameScreenId : uint
    {
        Initial = 0xA8C2D2D6,
        Framework = 0xCB76E58A,
        MainMenu = 0x4FC3DCE1,
        WorldMapLoading = 0x6CDF762A,
        WorldMap = 0xD48E8478,
        Gameplay = 0xF9D0FE7D,
        CheckpointScore = 0x7A2ABE82,
        LevelStats = 0xA61A5952,
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct ITFString
    {
        public uint String8Ptr;
        public uint String16Ptr;
        public int Length;
        public uint Uint_0C;
        public uint Uint_10;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct Map
    {
        public uint Uint_00;
        public uint HeaderNodePtr;
        public uint Ptr_08;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MapNode<TKey, TValue>
        where TKey : unmanaged
        where TValue : unmanaged
    {
        public uint LeftPointer;
        public uint ParentPointer;
        public uint RightPointer;
        public TKey Key;
        public TValue Value;
        public byte Color;
        [MarshalAs(UnmanagedType.I1)]
        public bool End;
    }

    #endregion
}