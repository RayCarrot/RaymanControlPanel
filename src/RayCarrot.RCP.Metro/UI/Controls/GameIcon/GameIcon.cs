using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace RayCarrot.RCP.Metro;

public class GameIcon : Control
{
    #region Source

    public ImageSource Source
    {
        get => (ImageSource)GetValue(SourceProperty);
        set => SetValue(SourceProperty, value);
    }

    public static readonly DependencyProperty SourceProperty = DependencyProperty.Register(
        nameof(Source), typeof(ImageSource), typeof(GameIcon), new PropertyMetadata(default(ImageSource)));

    #endregion

    #region IconSize

    public GameIconSize IconSize
    {
        get => (GameIconSize)GetValue(IconSizeProperty);
        set => SetValue(IconSizeProperty, value);
    }

    public static readonly DependencyProperty IconSizeProperty = DependencyProperty.Register(
        nameof(IconSize), typeof(GameIconSize), typeof(GameIcon), new PropertyMetadata(GameIconSize.Largest, OnIconSizeChanged));

    private static void OnIconSizeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        GameIcon gameIcon = (GameIcon)d;
        GameIconSize iconSize = (GameIconSize)e.NewValue;

        // Set the width and height
        gameIcon.IconWidth = gameIcon.IconHeight = iconSize switch
        {
            GameIconSize.Largest => 128,
            GameIconSize.Large => 96,
            GameIconSize.Medium => 64,
            GameIconSize.Small => 48,
            GameIconSize.Smallest => 32,
            _ => throw new ArgumentOutOfRangeException()
        };
        
        // Set the corner radius
        gameIcon.CornerRadius = new CornerRadius(iconSize switch
        {
            GameIconSize.Largest => 12,
            GameIconSize.Large => 9,
            GameIconSize.Medium => 6,
            GameIconSize.Small => 4,
            GameIconSize.Smallest => 2,
            _ => throw new ArgumentOutOfRangeException()
        });
    }

    #endregion

    #region IconWidth

    public double IconWidth
    {
        get => (double)GetValue(IconWidthProperty);
        private set => SetValue(IconWidthPropertyKey, value);
    }

    private static readonly DependencyPropertyKey IconWidthPropertyKey =
        DependencyProperty.RegisterReadOnly(nameof(IconWidth), typeof(double), typeof(GameIcon), new FrameworkPropertyMetadata(128d));

    public static readonly DependencyProperty IconWidthProperty = IconWidthPropertyKey.DependencyProperty;

    #endregion

    #region IconHeight

    public double IconHeight
    {
        get => (double)GetValue(IconHeightProperty);
        private set => SetValue(IconHeightPropertyKey, value);
    }

    private static readonly DependencyPropertyKey IconHeightPropertyKey =
        DependencyProperty.RegisterReadOnly(nameof(IconHeight), typeof(double), typeof(GameIcon), new FrameworkPropertyMetadata(128d));

    public static readonly DependencyProperty IconHeightProperty = IconHeightPropertyKey.DependencyProperty;

    #endregion

    #region CornerRadius

    public CornerRadius CornerRadius
    {
        get => (CornerRadius)GetValue(CornerRadiusProperty);
        private set => SetValue(CornerRadiusPropertyKey, value);
    }

    private static readonly DependencyPropertyKey CornerRadiusPropertyKey =
        DependencyProperty.RegisterReadOnly(nameof(CornerRadius), typeof(CornerRadius), typeof(GameIcon), new FrameworkPropertyMetadata(new CornerRadius(12d)));

    public static readonly DependencyProperty CornerRadiusProperty = CornerRadiusPropertyKey.DependencyProperty;

    #endregion

    public enum GameIconSize
    {
        /// <summary>
        /// 128x128
        /// </summary>
        Largest,

        /// <summary>
        /// 96x96
        /// </summary>
        Large,

        /// <summary>
        /// 64x64
        /// </summary>
        Medium,

        /// <summary>
        /// 48x48
        /// </summary>
        Small,

        /// <summary>
        /// 32x32
        /// </summary>
        Smallest,
    }
}