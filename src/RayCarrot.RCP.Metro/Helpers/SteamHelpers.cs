using System.IO;
using System.Text;
using BinarySerializer;

namespace RayCarrot.RCP.Metro;

public static class SteamHelpers
{
    public static string GetStorePageURL(string steamId)
    {
        return $"https://store.steampowered.com/app/{steamId}";
    }

    public static string GetCommunityPageURL(string steamId)
    {
        return $"https://steamcommunity.com/app/{steamId}";
    }

    public static string GetGameLaunchURI(string steamId)
    {
        return $@"steam://rungameid/{steamId}";
    }

    public static string GetGameLaunchURI(string steamId, string arguments)
    {
        return $@"steam://run/{steamId}//{arguments}/";
    }

    public static bool IsSteamStubDrmApplied(Stream stream)
    {
        using Reader reader = new(stream, leaveOpen: true);

        // Get the offset to the PE header
        stream.Position = 0x3C;
        int offset = reader.ReadInt32();
        
        // Go to the PE header
        stream.Position = offset;

        // Verify the signature
        int magic = reader.ReadInt32();
        if (magic != 0x00004550) // PE
            throw new Exception("Invalid exe file");

        // Read the values we want from the COFF header
        stream.Position += 2;
        short sectionsCount = reader.ReadInt16();
        stream.Position += 12;
        short optionalHeadersSize = reader.ReadInt16();
        stream.Position += 2;

        // Skip the optional headers
        stream.Position += optionalHeadersSize;

        // Enumerate the sections
        for (int i = 0; i < sectionsCount; i++)
        {
            // Read the section name
            string name = reader.ReadString(8, Encoding.UTF8);

            // Check if it's the .bind section
            if (name == ".bind")
                return true;
            
            // Skip the rest of the section header
            stream.Position += 32;
        }

        // No DRM found
        return false;
    }
}