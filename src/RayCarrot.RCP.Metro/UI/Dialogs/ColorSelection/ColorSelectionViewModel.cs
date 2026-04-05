using System.Windows.Media;

namespace RayCarrot.RCP.Metro;

/// <summary>
/// View model for a color selection dialog
/// </summary>
public class ColorSelectionViewModel : UserInputViewModel
{
    public Color SelectedColor { get; set; }
}