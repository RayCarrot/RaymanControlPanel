using System.Globalization;
using System.Windows.Media;
using Newtonsoft.Json;

namespace RayCarrot.RCP.Metro;

/// <summary>
/// Serializes a <see cref="Color"/> as a hex string
/// </summary>
public class ColorHexConverter : JsonConverter<Color?>
{
    public override bool CanRead => true;
    public override bool CanWrite => true;

    public override Color? ReadJson(JsonReader reader, Type objectType, Color? existingValue, bool hasExistingValue, JsonSerializer serializer)
    {
        string? hexString = reader.Value?.ToString();

        if (hexString == null)
            return null;

        if (!UInt32.TryParse(hexString, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out uint argbValue))
            return null;

        byte a = (byte)((argbValue & 0xFF000000) >> 24);
        byte r = (byte)((argbValue & 0x00FF0000) >> 16);
        byte g = (byte)((argbValue & 0x0000FF00) >> 8);
        byte b = (byte)((argbValue & 0x000000FF) >> 0);
        return Color.FromArgb(a, r, g, b);
    }

    public override void WriteJson(JsonWriter writer, Color? value, JsonSerializer serializer)
    {
        if (value is { } colorValue)
        {
            uint argbValue = 0;
            argbValue |= (uint)(colorValue.A << 24);
            argbValue |= (uint)(colorValue.R << 16);
            argbValue |= (uint)(colorValue.G << 8);
            argbValue |= (uint)(colorValue.B << 0);

            writer.WriteValue(argbValue.ToString("X8"));
        }
        else
        {
            writer.WriteNull();
        }
    }
}