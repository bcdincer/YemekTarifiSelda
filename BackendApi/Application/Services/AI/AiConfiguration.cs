using Microsoft.Extensions.Options;

namespace BackendApi.Application.Services.AI;

/// <summary>
/// AI servis konfigürasyonu (Options Pattern)
/// </summary>
public class AiConfiguration
{
    public const string SectionName = "AiConfiguration";
    
    public string Provider { get; set; } = "HuggingFace";
    
    public OpenAIConfig OpenAI { get; set; } = new();
    public HuggingFaceConfig HuggingFace { get; set; } = new();
}

public class OpenAIConfig
{
    public string ApiKey { get; set; } = string.Empty;
    public string ApiUrl { get; set; } = "https://api.openai.com/v1/chat/completions";
    public string Model { get; set; } = "gpt-4o-mini";
    public double Temperature { get; set; } = 0.2;
    public int MaxTokens { get; set; } = 1000;
}

public class HuggingFaceConfig
{
    public string ApiKey { get; set; } = string.Empty;
    public string ApiUrl { get; set; } = "https://api-inference.huggingface.co/v1/chat/completions";
    public string Model { get; set; } = "meta-llama/Meta-Llama-3-8B-Instruct";
    public bool Enabled { get; set; } = true;
    public double Temperature { get; set; } = 0.3;
    public int MaxTokens { get; set; } = 1000;
}


