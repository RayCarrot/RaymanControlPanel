using System.Windows.Media;

namespace RayCarrot.RCP.Metro;

/// <summary>
/// Result for a color selection dialog
/// </summary>
public class ColorSelectionResult : UserInputResult
{
    public Color SelectedColor { get; set; }
}