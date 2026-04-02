using System.Text.Json;
using System.Text.Json.Serialization;
using CommunityToolkit.Mvvm.ComponentModel;
using Hat.Models;

namespace Hat.Services;

/// <summary>
/// Persistent settings store using JSON file in %APPDATA%/Hat/settings.json.
/// Replaces macOS UserDefaults (SettingsManager in SettingsStore.swift).
/// </summary>
public partial class SettingsStore : ObservableObject
{
    private readonly string _settingsPath;
    private SettingsData _data;

    public SettingsStore()
    {
        var appData = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var hatDir = Path.Combine(appData, "Hat");
        Directory.CreateDirectory(hatDir);
        _settingsPath = Path.Combine(hatDir, "settings.json");
        _data = Load();
    }

    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
    // MARK: - Properties (matching SettingsManager from Swift)
    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

    public InferenceMode InferenceMode
    {
        get => _data.InferenceMode;
        set { _data.InferenceMode = value; Save(); OnPropertyChanged(); }
    }

    public CloudProvider SelectedProvider
    {
        get => _data.SelectedProvider;
        set { _data.SelectedProvider = value; Save(); OnPropertyChanged(); }
    }

    public string LocalModelName
    {
        get => _data.LocalModelName;
        set { _data.LocalModelName = value; Save(); OnPropertyChanged(); }
    }

    public string ApiEndpoint
    {
        get => _data.ApiEndpoint;
        set { _data.ApiEndpoint = value; Save(); OnPropertyChanged(); }
    }

    public string ApiModelName
    {
        get => _data.ApiModelName;
        set { _data.ApiModelName = value; Save(); OnPropertyChanged(); }
    }

    public string SystemPrompt
    {
        get => _data.SystemPrompt;
        set { _data.SystemPrompt = value; Save(); OnPropertyChanged(); }
    }

    public bool PlayNotifications
    {
        get => _data.PlayNotifications;
        set { _data.PlayNotifications = value; Save(); OnPropertyChanged(); }
    }

    public string AppThemeName
    {
        get => _data.AppThemeName;
        set { _data.AppThemeName = value; Save(); OnPropertyChanged(); }
    }

    public bool HasCompletedOnboarding
    {
        get => _data.HasCompletedOnboarding;
        set { _data.HasCompletedOnboarding = value; Save(); OnPropertyChanged(); }
    }

    // Appearance
    public double PopoverOpacity
    {
        get => _data.PopoverOpacity;
        set { _data.PopoverOpacity = value; Save(); OnPropertyChanged(); }
    }

    public double PopoverWidth
    {
        get => _data.PopoverWidth;
        set { _data.PopoverWidth = value; Save(); OnPropertyChanged(); }
    }

    public double PopoverHeight
    {
        get => _data.PopoverHeight;
        set { _data.PopoverHeight = value; Save(); OnPropertyChanged(); }
    }

    public bool PopoverVibrancy
    {
        get => _data.PopoverVibrancy;
        set { _data.PopoverVibrancy = value; Save(); OnPropertyChanged(); }
    }

    public bool PopoverStealthMode
    {
        get => _data.PopoverStealthMode;
        set { _data.PopoverStealthMode = value; Save(); OnPropertyChanged(); }
    }

    public double PopoverStealthHoverOpacity
    {
        get => _data.PopoverStealthHoverOpacity;
        set { _data.PopoverStealthHoverOpacity = value; Save(); OnPropertyChanged(); }
    }

    // Token tracking
    public int GlobalTotalTokens
    {
        get => _data.GlobalTotalTokens;
        set { _data.GlobalTotalTokens = value; Save(); OnPropertyChanged(); }
    }

    public int GlobalInputTokens
    {
        get => _data.GlobalInputTokens;
        set { _data.GlobalInputTokens = value; Save(); OnPropertyChanged(); }
    }

    public int GlobalOutputTokens
    {
        get => _data.GlobalOutputTokens;
        set { _data.GlobalOutputTokens = value; Save(); OnPropertyChanged(); }
    }

    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
    // MARK: - Computed helpers
    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

    /// Returns the API key for the currently selected provider.
    public string ApiKey => CredentialManager.Shared.LoadKey(SelectedProvider) ?? "";

    /// Returns the effective endpoint based on provider or custom setting.
    public string GetEffectiveEndpoint()
    {
        if (SelectedProvider == CloudProvider.Custom && !string.IsNullOrEmpty(ApiEndpoint))
            return ApiEndpoint;
        return SelectedProvider.DefaultEndpoint();
    }

    public void AddGlobalTokens(int input, int output)
    {
        _data.GlobalInputTokens += input;
        _data.GlobalOutputTokens += output;
        _data.GlobalTotalTokens += input + output;
        Save();
        OnPropertyChanged(nameof(GlobalTotalTokens));
        OnPropertyChanged(nameof(GlobalInputTokens));
        OnPropertyChanged(nameof(GlobalOutputTokens));
    }

    public void ResetGlobalTokens()
    {
        _data.GlobalTotalTokens = 0;
        _data.GlobalInputTokens = 0;
        _data.GlobalOutputTokens = 0;
        Save();
        OnPropertyChanged(nameof(GlobalTotalTokens));
        OnPropertyChanged(nameof(GlobalInputTokens));
        OnPropertyChanged(nameof(GlobalOutputTokens));
    }

    /// <summary>
    /// Save last used model for a specific provider.
    /// Port of CloudProvider.saveLastModel() from SettingsStore.swift.
    /// </summary>
    public void SaveLastModel(CloudProvider provider, string model)
    {
        _data.LastModelPerProvider[provider.CredentialKey()] = model;
        Save();
    }

    /// <summary>
    /// Load last used model for a specific provider.
    /// </summary>
    public string? LoadLastModel(CloudProvider provider)
    {
        return _data.LastModelPerProvider.TryGetValue(provider.CredentialKey(), out var model) ? model : null;
    }

    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
    // MARK: - Persistence
    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

    private void Save()
    {
        try
        {
            var json = JsonSerializer.Serialize(_data, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(_settingsPath, json);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to save settings: {ex.Message}");
        }
    }

    private SettingsData Load()
    {
        try
        {
            if (!File.Exists(_settingsPath))
                return new SettingsData();

            var json = File.ReadAllText(_settingsPath);
            return JsonSerializer.Deserialize<SettingsData>(json) ?? new SettingsData();
        }
        catch
        {
            return new SettingsData();
        }
    }

    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
    // MARK: - Internal data class
    // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

    private class SettingsData
    {
        [JsonConverter(typeof(JsonStringEnumConverter))]
        public InferenceMode InferenceMode { get; set; } = InferenceMode.Local;

        [JsonConverter(typeof(JsonStringEnumConverter))]
        public CloudProvider SelectedProvider { get; set; } = CloudProvider.Google;

        public string LocalModelName { get; set; } = "gemma3:4b";
        public string ApiEndpoint { get; set; } = "https://api.openai.com/v1/chat/completions";
        public string ApiModelName { get; set; } = "gpt-4o-mini";
        public string SystemPrompt { get; set; } = "Resposta direta. Pergunta: ";
        public bool PlayNotifications { get; set; } = true;
        public string AppThemeName { get; set; } = "Indigo";
        public bool HasCompletedOnboarding { get; set; }

        // Appearance
        public double PopoverOpacity { get; set; } = 1.0;
        public double PopoverWidth { get; set; } = 380.0;
        public double PopoverHeight { get; set; } = 480.0;
        public bool PopoverVibrancy { get; set; }
        public bool PopoverStealthMode { get; set; }
        public double PopoverStealthHoverOpacity { get; set; } = 0.4;

        // Token tracking
        public int GlobalTotalTokens { get; set; }
        public int GlobalInputTokens { get; set; }
        public int GlobalOutputTokens { get; set; }

        // Per-provider last model cache
        public Dictionary<string, string> LastModelPerProvider { get; set; } = new();
    }
}
