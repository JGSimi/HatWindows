using System.Windows.Media;

namespace Hat.Theme;

/// <summary>
/// All color tokens from the Hat design system.
/// Maps 1:1 from DesignSystem.swift Theme.Colors.
/// </summary>
public static class ThemeColors
{
    // Backgrounds — warm blacks
    public static readonly Color BackgroundLight = ColorFromHex("#F8F8F7");
    public static readonly Color BackgroundDark = ColorFromHex("#0C0C0E");

    public static readonly Color BackgroundSecondaryLight = ColorFromHex("#F2F2F1");
    public static readonly Color BackgroundSecondaryDark = ColorFromHex("#111114");

    // Surfaces — layered depth
    public static readonly Color SurfaceLight = Colors.White;
    public static readonly Color SurfaceDark = ColorFromHex("#1C1C1E");

    public static readonly Color SurfaceSecondaryLight = ColorFromHex("#F3F3F3");
    public static readonly Color SurfaceSecondaryDark = ColorFromAlpha(Colors.White, 0.055);

    public static readonly Color SurfaceTertiaryLight = ColorFromHex("#EDEDED");
    public static readonly Color SurfaceTertiaryDark = ColorFromAlpha(Colors.White, 0.035);

    public static readonly Color SurfaceElevatedLight = Colors.White;
    public static readonly Color SurfaceElevatedDark = ColorFromAlpha(Colors.White, 0.08);

    public static readonly Color SurfaceHoverLight = ColorFromAlpha(Colors.Black, 0.04);
    public static readonly Color SurfaceHoverDark = ColorFromAlpha(Colors.White, 0.06);

    // Borders
    public static readonly Color BorderLight = ColorFromAlpha(Colors.Black, 0.08);
    public static readonly Color BorderDark = ColorFromAlpha(Colors.White, 0.07);

    public static readonly Color BorderHighlightLight = ColorFromAlpha(Colors.Black, 0.12);
    public static readonly Color BorderHighlightDark = ColorFromAlpha(Colors.White, 0.10);

    // Text
    public static readonly Color TextPrimaryLight = ColorFromHex("#1B1B1E");
    public static readonly Color TextPrimaryDark = ColorFromHex("#EEEEF0");

    public static readonly Color TextSecondaryLight = ColorFromHex("#606066");
    public static readonly Color TextSecondaryDark = ColorFromHex("#C7C7CC");

    public static readonly Color TextMutedLight = ColorFromHex("#808085");
    public static readonly Color TextMutedDark = ColorFromHex("#9E9EA5");

    // Accent — for buttons (dark text on light, light text on dark)
    public static readonly Color AccentLight = ColorFromHex("#1B1B1E");
    public static readonly Color AccentDark = ColorFromHex("#EEEEF0");

    // Input
    public static readonly Color InputBackgroundLight = ColorFromHex("#F4F4F4");
    public static readonly Color InputBackgroundDark = ColorFromAlpha(Colors.White, 0.055);

    // Semantic
    public static readonly Color Success = ColorFromHex("#34C78C");
    public static readonly Color Error = ColorFromHex("#EF6363");
    public static readonly Color Warning = ColorFromHex("#F2B140");

    // Glass surfaces
    public static readonly Color GlassSurfaceLight = ColorFromAlpha(Colors.White, 0.45);
    public static readonly Color GlassSurfaceDark = ColorFromAlpha(Colors.White, 0.15);

    public static readonly Color GlassSurfaceSecondaryLight = ColorFromAlpha(Colors.White, 0.30);
    public static readonly Color GlassSurfaceSecondaryDark = ColorFromAlpha(Colors.White, 0.10);

    public static readonly Color GlassSurfaceElevatedLight = ColorFromAlpha(Colors.White, 0.55);
    public static readonly Color GlassSurfaceElevatedDark = ColorFromAlpha(Colors.White, 0.18);

    // Glass borders
    public static readonly Color GlassBorderLight = ColorFromAlpha(Colors.White, 0.60);
    public static readonly Color GlassBorderDark = ColorFromAlpha(Colors.White, 0.25);

    public static readonly Color GlassBorderSubtleLight = ColorFromAlpha(Colors.White, 0.35);
    public static readonly Color GlassBorderSubtleDark = ColorFromAlpha(Colors.White, 0.15);

    // Theme accent presets (from AppTheme enum)
    public static readonly Dictionary<string, (Color Primary, Color Hover)> ThemePresets = new()
    {
        ["Indigo"] = (ColorFromHex("#6366F1"), ColorFromHex("#818CF8")),
        ["Azul"] = (ColorFromHex("#3B82F6"), ColorFromHex("#60A5FA")),
        ["Roxo"] = (ColorFromHex("#935EEE"), ColorFromHex("#A37EF3")),
        ["Rosa"] = (ColorFromHex("#EC4899"), ColorFromHex("#F06CB1")),
        ["Vermelho"] = (ColorFromHex("#EF4444"), ColorFromHex("#F56B6B")),
        ["Laranja"] = (ColorFromHex("#F58522"), ColorFromHex("#F9A251")),
        ["Verde"] = (ColorFromHex("#22C55E"), ColorFromHex("#4ED780")),
        ["Azul Piscina"] = (ColorFromHex("#14B8A6"), ColorFromHex("#3ECCBE")),
        ["Monocromatico"] = (ColorFromHex("#99999F"), ColorFromHex("#B3B3B8")),
    };

    // Helper methods
    public static Color ColorFromHex(string hex)
    {
        hex = hex.TrimStart('#');
        byte r = Convert.ToByte(hex[..2], 16);
        byte g = Convert.ToByte(hex[2..4], 16);
        byte b = Convert.ToByte(hex[4..6], 16);
        return Color.FromRgb(r, g, b);
    }

    public static Color ColorFromAlpha(Color baseColor, double opacity)
    {
        return Color.FromArgb(
            (byte)(opacity * 255),
            baseColor.R,
            baseColor.G,
            baseColor.B
        );
    }
}
