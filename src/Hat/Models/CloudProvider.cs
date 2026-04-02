namespace Hat.Models;

public enum CloudProvider
{
    Google,
    OpenAI,
    Anthropic,
    Inception,
    OpenRouter,
    Custom
}

public static class CloudProviderExtensions
{
    public static string DisplayName(this CloudProvider provider) => provider switch
    {
        CloudProvider.Google => "Google Gemini",
        CloudProvider.OpenAI => "OpenAI ChatGPT",
        CloudProvider.Anthropic => "Anthropic Claude",
        CloudProvider.Inception => "Inception Mercury",
        CloudProvider.OpenRouter => "OpenRouter",
        CloudProvider.Custom => "Personalizado (Outros)",
        _ => "Google Gemini"
    };

    public static string ShortName(this CloudProvider provider) => provider switch
    {
        CloudProvider.Google => "Gemini",
        CloudProvider.OpenAI => "OpenAI",
        CloudProvider.Anthropic => "Claude",
        CloudProvider.Inception => "Mercury",
        CloudProvider.OpenRouter => "OpenRouter",
        CloudProvider.Custom => "Custom",
        _ => "Gemini"
    };

    public static string DefaultEndpoint(this CloudProvider provider) => provider switch
    {
        CloudProvider.Google => "https://generativelanguage.googleapis.com/v1beta/openai/chat/completions",
        CloudProvider.OpenAI => "https://api.openai.com/v1/chat/completions",
        CloudProvider.Anthropic => "https://api.anthropic.com/v1/messages",
        CloudProvider.Inception => "https://api.inceptionlabs.ai/v1/chat/completions",
        CloudProvider.OpenRouter => "https://openrouter.ai/api/v1/chat/completions",
        CloudProvider.Custom => "",
        _ => ""
    };

    public static string? ModelsEndpoint(this CloudProvider provider) => provider switch
    {
        CloudProvider.Google => "https://generativelanguage.googleapis.com/v1beta/openai/models",
        CloudProvider.OpenAI => "https://api.openai.com/v1/models",
        CloudProvider.Anthropic => "https://api.anthropic.com/v1/models",
        CloudProvider.Inception => "https://api.inceptionlabs.ai/v1/models",
        CloudProvider.OpenRouter => "https://openrouter.ai/api/v1/models",
        CloudProvider.Custom => null,
        _ => null
    };

    public static string CredentialKey(this CloudProvider provider) => provider switch
    {
        CloudProvider.Google => "apiKey_google",
        CloudProvider.OpenAI => "apiKey_openai",
        CloudProvider.Anthropic => "apiKey_anthropic",
        CloudProvider.Inception => "apiKey_inception",
        CloudProvider.OpenRouter => "apiKey_openrouter",
        CloudProvider.Custom => "apiKey_custom",
        _ => "apiKey_google"
    };
}
