using System.Windows;
using System.Windows.Media;

namespace Hat.Theme;

/// <summary>
/// Typography tokens mapped from SF Pro to Segoe UI Variable / Cascadia Code.
/// </summary>
public static class ThemeTypography
{
    // Primary font: Segoe UI Variable (Win11) fallback Segoe UI (Win10)
    public static readonly FontFamily PrimaryFont = new("Segoe UI Variable, Segoe UI");

    // Monospaced font: Cascadia Code (Win11) fallback Consolas
    public static readonly FontFamily MonoFont = new("Cascadia Code, Consolas");

    // Font sizes (matching DesignSystem.swift exactly)
    public const double LargeTitleSize = 26;
    public const double TitleSize = 20;
    public const double HeadingSize = 16;
    public const double SubheadingSize = 14;
    public const double BodyBoldSize = 13;
    public const double BodySize = 13;
    public const double BodySmallSize = 12.5;
    public const double BodyMonoSize = 12;
    public const double CaptionSize = 11;
    public const double CaptionBoldSize = 11;
    public const double SectionHeaderSize = 11;
    public const double MicroSize = 10;
    public const double CodeBlockSize = 12;

    // Font weights
    public static readonly FontWeight Bold = FontWeights.Bold;
    public static readonly FontWeight SemiBold = FontWeights.SemiBold;
    public static readonly FontWeight Medium = FontWeights.Medium;
    public static readonly FontWeight Regular = FontWeights.Regular;
}
