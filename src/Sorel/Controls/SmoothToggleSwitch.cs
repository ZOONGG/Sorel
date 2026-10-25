using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace Sorel.Controls;

// Bound/recycled rows take their checked position immediately. Only a user toggle animates.
public class SmoothToggleSwitch : CheckBox
{
    protected override void OnToggle()
    {
        ApplyTemplate();
        var thumb = Template?.FindName("Thumb", this) as FrameworkElement;
        var from = (thumb?.RenderTransform as TranslateTransform)?.X;
        base.OnToggle();
        if (!IsLoaded || thumb is null || from is null || !SystemParameters.ClientAreaAnimation ||
            Environment.GetEnvironmentVariable("SOREL_REDUCED_MOTION") == "1") return;

        var to = (thumb.RenderTransform as TranslateTransform)?.GetAnimationBaseValue(TranslateTransform.XProperty);
        if (to is not double target) return;
        var animation = new DoubleAnimation(from.Value, target, TimeSpan.FromMilliseconds(150))
        {
            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut },
            FillBehavior = FillBehavior.Stop
        };
        Storyboard.SetTarget(animation, thumb);
        Storyboard.SetTargetProperty(animation,
            new PropertyPath("(UIElement.RenderTransform).(TranslateTransform.X)"));
        var storyboard = new Storyboard();
        storyboard.Children.Add(animation);
        storyboard.Begin(this, HandoffBehavior.SnapshotAndReplace);
    }
}
