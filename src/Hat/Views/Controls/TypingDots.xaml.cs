using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace Hat.Views.Controls;

/// <summary>
/// Animated typing indicator: 3 dots cycling.
/// Port of MaeTypingDots from DesignSystem.swift.
/// 350ms interval, active dot: opacity 0.85 + scale 1.2.
/// </summary>
public partial class TypingDots : UserControl
{
    private readonly DispatcherTimer _timer;
    private int _activeIndex;
    private Ellipse[] _dots = null!;

    public TypingDots()
    {
        InitializeComponent();
        Loaded += OnLoaded;

        _timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(350) };
        _timer.Tick += OnTick;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        _dots = new[] { Dot0, Dot1, Dot2 };
        _timer.Start();
    }

    private void OnTick(object? sender, EventArgs e)
    {
        _activeIndex = (_activeIndex + 1) % 3;
        for (int i = 0; i < 3; i++)
        {
            var isActive = i == _activeIndex;
            _dots[i].Opacity = isActive ? 0.85 : 0.25;
            _dots[i].RenderTransform = new ScaleTransform(
                isActive ? 1.2 : 1.0,
                isActive ? 1.2 : 1.0,
                2.5, 2.5);
        }
    }
}
