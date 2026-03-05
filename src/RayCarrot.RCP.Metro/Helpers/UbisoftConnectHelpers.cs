using System.IO;
using RayCarrot.RCP.Metro.Games.Clients;
using RayCarrot.RCP.Metro.Games.Clients.UbisoftConnect;
using RayCarrot.RCP.Metro.Games.Components;

namespace RayCarrot.RCP.Metro;

public static class UbisoftConnectHelpers
{
    /// <summary>
    /// Gets the Ubisoft Connect save directory path for the game. If none is found then <see cref="FileSystemPath.EmptyPath"/> is returned.
    /// </summary>
    /// <param name="gameInstallation">The game to get the path for</param>
    /// <returns>The save directory path or an empty path if none was found</returns>
    public static FileSystemPath GetSaveDirectory(GameInstallation gameInstallation)
    {
        GameClientInstallation? gameClientInstallation = Services.GameClients.GetAttachedGameClient(gameInstallation);
        string gameId = gameInstallation.GetRequiredComponent<UbisoftConnectGameClientComponent>().GameId;

        // Attempt to get the path from the game client installation
        if (gameClientInstallation?.GameClientDescriptor is UbisoftConnectGameClientDescriptor)
        {
            string? userId = gameClientInstallation.GetValue<string>(GameClientDataKey.UbisoftConnect_UserId);

            if (userId != null)
                return gameClientInstallation.InstallLocation.Directory + "savegames" + userId + gameId;
        }

        // Fallback to first added Ubisoft Connect game client installation
        gameClientInstallation = Services.GameClients.GetInstalledGameClients().
            FirstOrDefault(x => x.GameClientDescriptor is UbisoftConnectGameClientDescriptor);

        if (gameClientInstallation != null)
        {
            string? userId = gameClientInstallation.GetValue<string>(GameClientDataKey.UbisoftConnect_UserId);

            if (userId != null)
                return gameClientInstallation.InstallLocation.Directory + "savegames" + userId + gameId;
        }

        // Fallback to the default location
        FileSystemPath saveGamesDir = @"C:\Program Files (x86)\Ubisoft\Ubisoft Game Launcher\savegames";

        if (saveGamesDir.DirectoryExists)
        {
            string[] subDirs = Directory.GetDirectories(saveGamesDir);

            if (subDirs.Length > 0)
            {
                FileSystemPath userSaveDir = subDirs[0];
                return userSaveDir + gameId;
            }
        }

        return FileSystemPath.EmptyPath;
    }

    /// <summary>
    /// Gets the Ubisoft Connect save directory path for the game from a base save path. If none is found then <see cref="FileSystemPath.EmptyPath"/> is returned.
    /// </summary>
    /// <param name="gameInstallation">The game to get the path for</param>
    /// <param name="savePath">The base path for the save</param>
    /// <returns>The save directory path or an empty path if none was found</returns>
    public static FileSystemPath GetSaveDirectory(GameInstallation gameInstallation, FileSystemPath savePath)
    {
        GameClientInstallation? gameClientInstallation = Services.GameClients.GetAttachedGameClient(gameInstallation);

        string? userId = null;

        // Attempt to get the user ID from the game client installation
        if (gameClientInstallation?.GameClientDescriptor is UbisoftConnectGameClientDescriptor)
            userId = gameClientInstallation.GetValue<string>(GameClientDataKey.UbisoftConnect_UserId);

        // Fallback to get the ID from the first added Ubisoft Connect game client installation
        if (userId == null || !(savePath + userId).DirectoryExists)
        {
            gameClientInstallation = Services.GameClients.GetInstalledGameClients().
                FirstOrDefault(x => x.GameClientDescriptor is UbisoftConnectGameClientDescriptor);
            
            if (gameClientInstallation != null)
                userId = gameClientInstallation.GetValue<string>(GameClientDataKey.UbisoftConnect_UserId);
        }

        // Fallback to find from the save path
        if (userId == null || !(savePath + userId).DirectoryExists)
        {
            if (savePath.DirectoryExists)
            {
                string[] subDirs = Directory.GetDirectories(savePath);
                if (subDirs.Length > 0)
                    userId = Path.GetFileName(subDirs[0]);
            }
        }

        if (userId == null)
            return FileSystemPath.EmptyPath;
        else
            return savePath + userId;
    }

    public static string GetStorePageURL(string productId)
    {
        return $"https://store.ubisoft.com/{productId}.html";
    }

    public static string GetGameLaunchURI(string gameId)
    {
        return $@"uplay://launch/{gameId}/0";
    }
}