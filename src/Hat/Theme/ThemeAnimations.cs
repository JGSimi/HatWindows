using System.Windows;
using System.Windows.Media.Animation;

namespace Hat.Theme;

/// <summary>
/// Animation constants and helpers from DesignSystem.swift Theme.Animation.
/// </summary>
public static class ThemeAnimations
{
    // Durations
    public static readonly Duration Fast = new(TimeSpan.FromSeconds(0.15));
    public static readonly Duration Normal = new(TimeSpan.FromSeconds(0.25));
    public static readonly Duration Slow = new(TimeSpan.FromSeconds(0.4));

    // Spring-equivalent durations (WPF uses easing functions instead of springs)
    public static readonly Duration Smooth = new(TimeSpan.FromSeconds(0.3));
    public static readonly Duration Snappy = new(TimeSpan.FromSeconds(0.22));
    public static readonly Duration Gentle = new(TimeSpan.FromSeconds(0.35));
    public static readonly Duration QuickSnap = new(TimeSpan.FromSeconds(0.16));

    // Easing functions that approximate SwiftUI spring damping behavior
    // High damping (0.86-0.92) = smooth deceleration = QuadraticEase or CubicEase
    public static readonly IEasingFunction SmoothEase = new QuadraticEase { EasingMode = EasingMode.EaseOut };
    public static readonly IEasingFunction SnappyEase = new CubicEase { EasingMode = EasingMode.EaseOut };
    public static readonly IEasingFunction GentleEase = new SineEase { EasingMode = EasingMode.EaseInOut };
    public static readonly IEasingFunction QuickSnapEase = new ExponentialEase { Exponent = 4, EasingMode = EasingMode.EaseOut };

    // Pulse animation
    public const double PulseMinScale = 0.95;
    public const double PulseMaxOpacity = 1.0;
    public const double PulseMinOpacity = 0.65;
    public const double PulseDuration = 1.6;

    // Press effect
    public const double PressScale = 0.96;
    public const double PressOpacity = 0.8;

    // Stagger delay base
    public const double StaggerBaseDelay = 0.04;

    // Typing dots
    public const double TypingDotInterval = 0.35;

    /// <summary>
    /// Creates a DoubleAnimation with the specified parameters.
    /// </summary>
    public static DoubleAnimation CreateAnimation(double to, Duration duration, IEasingFunction? easing = null)
    {
        return new DoubleAnimation
        {
            To = to,
            Duration = duration,
            EasingFunction = easing ?? SmoothEase
        };
    }

    /// <summary>
    /// Creates a stagger delay for sequential item appearance.
    /// </summary>
    public static TimeSpan StaggerDelay(int index, double baseDelay = StaggerBaseDelay)
    {
        return TimeSpan.FromSeconds(index * baseDelay);
    }
}
