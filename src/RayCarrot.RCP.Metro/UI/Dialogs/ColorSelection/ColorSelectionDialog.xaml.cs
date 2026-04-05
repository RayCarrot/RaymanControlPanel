using System.Windows;

namespace RayCarrot.RCP.Metro;

/// <summary>
/// Interaction logic for ColorSelectionDialog.xaml
/// </summary>
public partial class ColorSelectionDialog : WindowContentControl, IDialogWindowControl<ColorSelectionViewModel, ColorSelectionResult>
{ 
    #region Constructor

    public ColorSelectionDialog(ColorSelectionViewModel vm)
    {
        InitializeComponent();
        ViewModel = vm;
        DataContext = ViewModel;
        CanceledByUser = true;
    }

    #endregion

    #region Public Properties

    /// <summary>
    /// Indicates if the dialog was canceled by the user, default is true
    /// </summary>
    public bool CanceledByUser { get; set; }

    /// <summary>
    /// The view model
    /// </summary>
    public ColorSelectionViewModel ViewModel { get; }

    #endregion

    #region Protected Methods

    protected override void WindowAttached()
    {
        base.WindowAttached();

        WindowInstance.Icon = GenericIconKind.Window_ColorSelection;
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// Gets the current result for the dialog
    /// </summary>
    /// <returns>The result</returns>
    public ColorSelectionResult GetResult()
    {
        return new ColorSelectionResult()
        {
            CanceledByUser = CanceledByUser,
            SelectedColor = ViewModel.SelectedColor,
        };
    }

    #endregion

    #region Event Handlers

    private void OKButton_Click(object sender, RoutedEventArgs e)
    {
        CanceledByUser = false;

        // Close the dialog
        WindowInstance.Close();
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        // Close the dialog
        WindowInstance.Close();
    }

    #endregion
}