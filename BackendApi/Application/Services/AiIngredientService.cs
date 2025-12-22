using BackendApi.Application.Services.AI;
using Microsoft.Extensions.Options;
using System.Net.Http.Headers;

namespace BackendApi.Application.Services;

public class AiIngredientService : IAiIngredientService
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<AiIngredientService> _logger;
    private readonly AiConfiguration _config;
    private readonly AiRequestBuilder _requestBuilder;
    
    // Rate limiting için
    private static DateTime? _lastRequestTime;
    private static readonly SemaphoreSlim _rateLimitSemaphore = new SemaphoreSlim(1, 1);

    public AiIngredientService(
        IHttpClientFactory httpClientFactory, 
        IOptions<AiConfiguration> configOptions, 
        ILogger<AiIngredientService> logger)
    {
        _httpClient = httpClientFactory.CreateClient();
        _logger = logger;
        _config = configOptions.Value;
        _requestBuilder = new AiRequestBuilder(_config);
        
        _httpClient.DefaultRequestHeaders.Add("User-Agent", AiServiceConstants.UserAgent);
    }

    public async Task<List<string>> AdjustIngredientsAsync(List<string> ingredients, int originalServings, int newServings)
    {
        // Input validation
        if (ingredients == null || ingredients.Count == 0)
        {
            _logger.LogWarning("Empty ingredients list provided");
            return new List<string>();
        }

        if (originalServings <= 0 || newServings <= 0)
        {
            _logger.LogWarning("Invalid servings: Original={Original}, New={New}", originalServings, newServings);
            return ingredients; // Geçersiz değerlerde orijinal listeyi döndür
        }

        _logger.LogInformation("=== AI INGREDIENT ADJUSTMENT STARTED ===");
        _logger.LogInformation("AI Provider: {Provider}", _config.Provider);
        _logger.LogInformation("Original Servings: {Original}, New Servings: {New}", originalServings, newServings);
        _logger.LogInformation("Ingredients Count: {Count}", ingredients.Count);
        
        var useHuggingFace = _config.Provider.Equals("HuggingFace", StringComparison.OrdinalIgnoreCase);
        return await AdjustWithProviderAsync(ingredients, originalServings, newServings, useHuggingFace);
    }

    private async Task<List<string>> AdjustWithProviderAsync(
        List<string> ingredients, 
        int originalServings, 
        int newServings, 
        bool useHuggingFace)
    {
        var providerName = useHuggingFace ? "Hugging Face" : "OpenAI";
        _logger.LogInformation("Using {Provider} AI", providerName);

        // Hugging Face kontrolü
        if (useHuggingFace)
        {
            if (!_config.HuggingFace.Enabled || string.IsNullOrEmpty(_config.HuggingFace.ApiKey))
            {
                _logger.LogWarning("Hugging Face not configured or disabled, using fallback");
                return AdjustIngredientsFallback(ingredients, originalServings, newServings);
            }
        }
        else
        {
            if (string.IsNullOrEmpty(_config.OpenAI.ApiKey))
            {
                _logger.LogWarning("OpenAI API key not configured, using fallback");
                return AdjustIngredientsFallback(ingredients, originalServings, newServings);
            }
        }

        try
        {
            // Prompt oluştur
            var userPrompt = PromptTemplates.BuildIngredientScalingPrompt(
                originalServings, 
                newServings, 
                ingredients,
                TurkishNumberConverter.ToNaturalExpression);

            // Rate limiting
            await WaitForRateLimit();

            // Request oluştur
            var request = useHuggingFace 
                ? _requestBuilder.BuildHuggingFaceRequest(userPrompt)
                : _requestBuilder.BuildOpenAIRequest(userPrompt);

            // Retry mekanizması ile istek gönder
            HttpResponseMessage? response = null;
            try
            {
                response = await SendRequestWithRetryAsync(request, providerName);

                if (response == null || !response.IsSuccessStatusCode)
                {
                    await HandleErrorResponseAsync(response, providerName);
                    return AdjustIngredientsFallback(ingredients, originalServings, newServings);
                }

                // Response'u parse et
                var responseContent = await response.Content.ReadAsStringAsync();
                _logger.LogInformation("{Provider} Raw Response: {Response}", providerName, responseContent);

                var adjustedIngredients = AiResponseParser.ParseChatCompletionsResponse(responseContent, _logger);

                if (adjustedIngredients.Count == ingredients.Count)
                {
                    _logger.LogInformation("=== {Provider} SUCCESS ===", providerName);
                    _logger.LogInformation("Adjusted Ingredients: {Ingredients}", string.Join(" | ", adjustedIngredients));
                    return adjustedIngredients;
                }

                _logger.LogWarning("{Provider} response could not be parsed correctly (expected {Expected}, got {Actual}), using fallback",
                    providerName, ingredients.Count, adjustedIngredients.Count);
                return AdjustIngredientsFallback(ingredients, originalServings, newServings);
            }
            finally
            {
                // Request'i dispose et
                request?.Dispose();
                response?.Dispose();
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calling {Provider} API", providerName);
            return AdjustIngredientsFallback(ingredients, originalServings, newServings);
        }
    }

    private async Task<HttpResponseMessage?> SendRequestWithRetryAsync(HttpRequestMessage request, string providerName)
    {
        int retryCount = 0;

        while (retryCount < AiServiceConstants.MaxRetries)
        {
            try
            {
                var response = await _httpClient.SendAsync(request);

                if (response.IsSuccessStatusCode)
                {
                    return response;
                }

                // 429 hatası ise retry yap
                if (response.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
                {
                    retryCount++;
                    var retryAfter = GetRetryAfterSeconds(response);
                    _logger.LogWarning("{Provider} API rate limit exceeded. Retry {RetryCount}/{MaxRetries} after {Seconds} seconds",
                        providerName, retryCount, AiServiceConstants.MaxRetries, retryAfter);

                    if (retryCount < AiServiceConstants.MaxRetries)
                    {
                        await Task.Delay(TimeSpan.FromSeconds(retryAfter));
                        continue;
                    }
                    else
                    {
                        _logger.LogError("{Provider} API rate limit exceeded after {RetryCount} retries", providerName, retryCount);
                        return response;
                    }
                }

                return response;
            }
            catch (Exception ex)
            {
                retryCount++;
                if (retryCount >= AiServiceConstants.MaxRetries)
                {
                    _logger.LogError(ex, "{Provider} API request failed after {RetryCount} retries", providerName, retryCount);
                    return null;
                }

                var delay = (int)Math.Pow(2, retryCount);
                _logger.LogWarning("{Provider} API request failed, retrying in {Delay} seconds (attempt {RetryCount}/{MaxRetries})",
                    providerName, delay, retryCount, AiServiceConstants.MaxRetries);
                await Task.Delay(TimeSpan.FromSeconds(delay));
            }
        }

        return null;
    }

    private async Task HandleErrorResponseAsync(HttpResponseMessage? response, string providerName)
    {
        if (response == null) return;

        var errorContent = await response.Content.ReadAsStringAsync();
        _logger.LogWarning("{Provider} API error: {StatusCode} - {Error}", providerName, response.StatusCode, errorContent);

        // LLaMA license hatası kontrolü
        if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized ||
            response.StatusCode == System.Net.HttpStatusCode.Forbidden ||
            response.StatusCode == System.Net.HttpStatusCode.Gone)
        {
            _logger.LogError("{Provider} authentication/license error. LLaMA modelleri için license kabul etmeniz gerekiyor: https://huggingface.co/{Model}",
                providerName, _config.HuggingFace.Model);
        }
    }

    private List<string> AdjustIngredientsFallback(List<string> ingredients, int originalServings, int newServings)
    {
        _logger.LogInformation("=== FALLING BACK TO MATHEMATICAL CALCULATION ===");
        var multiplier = (double)newServings / originalServings;
        var adjusted = new List<string>();

        foreach (var ingredient in ingredients)
        {
            var adjustedIngredient = AdjustSingleIngredient(ingredient, multiplier);
            adjusted.Add(adjustedIngredient);
        }

        return adjusted;
    }

    private string AdjustSingleIngredient(string ingredient, double multiplier)
    {
        var fractionMap = new Dictionary<string, double>
        {
            { "yarım", 0.5 },
            { "yarı", 0.5 },
            { "çeyrek", 0.25 },
            { "üç çeyrek", 0.75 },
            { "dörtte üç", 0.75 },
            { "bir buçuk", 1.5 },
            { "iki buçuk", 2.5 },
            { "üç buçuk", 3.5 }
        };

        var text = ingredient;
        var originalText = text;

        // Önce kesir ifadelerini sayıya çevir ve hesapla
        foreach (var (fraction, value) in fractionMap.OrderByDescending(x => x.Key.Length))
        {
            if (text.ToLower().Contains(fraction.ToLower()))
            {
                var newValue = value * multiplier;
                text = text.Replace(fraction, TurkishNumberConverter.ToNaturalExpression(newValue), StringComparison.OrdinalIgnoreCase);
                break;
            }
        }

        // Eğer kesir ifadesi bulunamadıysa, sayısal değerleri bul ve çarp
        if (text == originalText)
        {
            text = System.Text.RegularExpressions.Regex.Replace(text, @"(\d+\.?\d*)\s+([a-zA-ZğüşıöçĞÜŞİÖÇ\s]+?)(?=\s|$|,|\.)", match =>
            {
                var num = double.Parse(match.Groups[1].Value);
                var unit = match.Groups[2].Value.Trim();
                var newNum = num * multiplier;
                var formattedNum = TurkishNumberConverter.ToNaturalExpression(newNum);
                return $"{formattedNum} {unit}";
            });
        }

        return text;
    }

    private async Task WaitForRateLimit()
    {
        await _rateLimitSemaphore.WaitAsync();
        try
        {
            if (_lastRequestTime.HasValue)
            {
                var timeSinceLastRequest = DateTime.UtcNow - _lastRequestTime.Value;
                var waitTime = TimeSpan.FromSeconds(AiServiceConstants.MinRequestIntervalSeconds) - timeSinceLastRequest;

                if (waitTime > TimeSpan.Zero)
                {
                    _logger.LogDebug("Rate limiting: Waiting {Seconds} seconds before next request", waitTime.TotalSeconds);
                    await Task.Delay(waitTime);
                }
            }

            _lastRequestTime = DateTime.UtcNow;
        }
        finally
        {
            _rateLimitSemaphore.Release();
        }
    }

    private int GetRetryAfterSeconds(HttpResponseMessage response)
    {
        if (response.Headers.TryGetValues("Retry-After", out var retryAfterValues))
        {
            if (int.TryParse(retryAfterValues.FirstOrDefault(), out var retryAfter))
            {
                return retryAfter;
            }
        }

        return AiServiceConstants.DefaultRetryAfterSeconds;
    }
}
