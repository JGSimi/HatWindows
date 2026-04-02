namespace Hat.Models;

public enum InferenceMode
{
    Local,  // Modelos Locais (Ollama)
    Api     // API na Nuvem
}

public static class InferenceModeExtensions
{
    public static string DisplayName(this InferenceMode mode) => mode switch
    {
        InferenceMode.Local => "Modelos Locais (Ollama)",
        InferenceMode.Api => "API na Nuvem (Google, OpenAI, etc)",
        _ => "Local"
    };
}
