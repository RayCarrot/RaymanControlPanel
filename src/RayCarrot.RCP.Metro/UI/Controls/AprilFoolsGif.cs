using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Animation;
using XamlAnimatedGif;

namespace RayCarrot.RCP.Metro;

public class AprilFoolsGif : Image
{
    public AprilFoolsGif()
    {
        // Hide if not April 1st (unless in design mode)
        if (!DesignerProperties.GetIsInDesignMode(this) && (DateTime.Now.Month != 4 || DateTime.Now.Day != 1))
        {
            Visibility = Visibility.Collapsed;
            IsAvailable = false;
        }
        else
        {
            IsAvailable = true;
        }

        Loaded += (_, _) =>
        {
            if (ForceShow)
                ReInit();
        };

#if DEBUG
        AnimationBehavior.SetAnimateInDesignMode(this, true);
#endif
    }

    private Thickness _savedMargin;

    public static bool ForceShow { get; set; }

    public bool IsAvailable { get; set; }

    public AprilFoolsAsset Asset
    {
        get => (AprilFoolsAsset)GetValue(AssetProperty);
        set => SetValue(AssetProperty, value);
    }

    public static readonly DependencyProperty AssetProperty = DependencyProperty.Register(nameof(Asset), typeof(AprilFoolsAsset), typeof(AprilFoolsGif), new FrameworkPropertyMetadata(OnAssetChanged));

    public Thickness FlashMargin
    {
        get => (Thickness)GetValue(FlashMarginProperty);
        set => SetValue(FlashMarginProperty, value);
    }

    public static readonly DependencyProperty FlashMarginProperty = DependencyProperty.Register(nameof(FlashMargin), typeof(Thickness), typeof(AprilFoolsGif));

    private static void OnAssetChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is AprilFoolsGif gif) 
            gif.ReInit();
    }

    private void OnMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        MouseLeftButtonDown -= OnMouseLeftButtonDown;

        // Set the gif to the flash and play only once
        AnimationBehavior.SetSourceUri(this, new Uri(AprilFoolsAsset.Flash.GetAssetPath()));
        AnimationBehavior.SetRepeatBehavior(this, new RepeatBehavior(1));
        
        // Replace the margin
        _savedMargin = Margin;
        Margin = FlashMargin;
        
        // Reset once done
        AnimationBehavior.AddAnimationCompletedHandler(this, (_, _) =>
        {
            Visibility = Visibility.Collapsed;
            Margin = _savedMargin;
        });
    }

    public void ReInit()
    {
        if (!IsAvailable && !ForceShow)
            return;

        // Make visible
        Visibility = Visibility.Visible;
        
        // Subscribe to when the mouse is clicked
        MouseLeftButtonDown -= OnMouseLeftButtonDown;
        MouseLeftButtonDown += OnMouseLeftButtonDown;

        // Set the gif asset
        AnimationBehavior.SetSourceUri(this, new Uri(Asset.GetAssetPath()));
        AnimationBehavior.SetRepeatBehavior(this, RepeatBehavior.Forever);
    }
}