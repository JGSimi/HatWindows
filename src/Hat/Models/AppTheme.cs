namespace Hat.Models;

/// <summary>
/// 9 color theme presets (from SettingsStore.swift AppTheme enum).
/// </summary>
public enum AppTheme
{
    Indigo,
    Azul,
    Roxo,
    Rosa,
    Vermelho,
    Laranja,
    Verde,
    AzulPiscina,
    Monocromatico
}

public static class AppThemeExtensions
{
    public static string DisplayName(this AppTheme theme) => theme switch
    {
        AppTheme.Indigo => "Indigo",
        AppTheme.Azul => "Azul",
        AppTheme.Roxo => "Roxo",
        AppTheme.Rosa => "Rosa",
        AppTheme.Vermelho => "Vermelho",
        AppTheme.Laranja => "Laranja",
        AppTheme.Verde => "Verde",
        AppTheme.AzulPiscina => "Azul Piscina",
        AppTheme.Monocromatico => "Monocromatico",
        _ => "Indigo"
    };

    public static string HexColor(this AppTheme theme) => theme switch
    {
        AppTheme.Indigo => "#6366F1",
        AppTheme.Azul => "#3B82F6",
        AppTheme.Roxo => "#935EEE",
        AppTheme.Rosa => "#EC4899",
        AppTheme.Vermelho => "#EF4444",
        AppTheme.Laranja => "#F58522",
        AppTheme.Verde => "#22C55E",
        AppTheme.AzulPiscina => "#14B8A6",
        AppTheme.Monocromatico => "#99999F",
        _ => "#6366F1"
    };
}
