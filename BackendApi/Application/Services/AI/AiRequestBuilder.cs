using System.Text;
using System.Text.Json;

namespace BackendApi.Application.Services.AI;

/// <summary>
/// AI isteklerini oluşturan builder sınıfı
/// </summary>
public class AiRequestBuilder
{
    private readonly AiConfiguration _config;

    public AiRequestBuilder(AiConfiguration config)
    {
        _config = config;
    }

    public HttpRequestMessage BuildOpenAIRequest(string userPrompt)
    {
        var requestBody = new
        {
            model = _config.OpenAI.Model,
            messages = new[]
            {
                new { role = "system", content = PromptTemplates.SystemMessage },
                new { role = "user", content = userPrompt }
            },
            temperature = _config.OpenAI.Temperature,
            max_tokens = _config.OpenAI.MaxTokens
        };

        var json = JsonSerializer.Serialize(requestBody, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var request = new HttpRequestMessage(HttpMethod.Post, _config.OpenAI.ApiUrl);
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _config.OpenAI.ApiKey);
        request.Content = content;

        return request;
    }

    public HttpRequestMessage BuildHuggingFaceRequest(string userPrompt)
    {
        var requestBody = new
        {
            model = _config.HuggingFace.Model,
            messages = new[]
            {
                new { role = "system", content = PromptTemplates.SystemMessage },
                new { role = "user", content = userPrompt }
            },
            temperature = _config.HuggingFace.Temperature,
            max_tokens = _config.HuggingFace.MaxTokens
        };

        var json = JsonSerializer.Serialize(requestBody, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var request = new HttpRequestMessage(HttpMethod.Post, _config.HuggingFace.ApiUrl);
        request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _config.HuggingFace.ApiKey);
        request.Content = content;

        return request;
    }
}


