using System.Text.Json;

namespace BackendApi.Application.Services.AI;

/// <summary>
/// AI yanıtlarını parse eden sınıf
/// </summary>
public class AiResponseParser
{
    public static List<string> ParseChatCompletionsResponse(string responseContent, ILogger logger)
    {
        try
        {
            var responseJson = JsonDocument.Parse(responseContent);
            
            // OpenAI/Hugging Face Chat Completions formatı: {"choices": [{"message": {"content": "..."}}]}
            var generatedText = responseJson.RootElement
                .GetProperty("choices")[0]
                .GetProperty("message")
                .GetProperty("content")
                .GetString();

            if (string.IsNullOrEmpty(generatedText))
            {
                logger.LogWarning("AI returned empty response");
                return new List<string>();
            }

            return ParseIngredientList(generatedText, logger);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error parsing AI response");
            return new List<string>();
        }
    }

    private static List<string> ParseIngredientList(string aiResponse, ILogger logger)
    {
        var lines = aiResponse.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        var ingredients = new List<string>();

        logger.LogInformation("=== PARSING AI RESPONSE ===");
        logger.LogInformation("Total lines: {LineCount}", lines.Length);

        foreach (var line in lines)
        {
            var trimmed = line.Trim();
            
            // Boş satırları atla
            if (string.IsNullOrWhiteSpace(trimmed))
            {
                logger.LogDebug("Skipping empty line");
                continue;
            }
            
            // Başlık satırlarını atla
            if (trimmed.Contains("kişilik") || trimmed.Contains("malzeme listesi") || 
                trimmed.ToLower().Contains("güncellenmiş") || trimmed.StartsWith("Görev") ||
                trimmed.StartsWith("Malzemeler:") || trimmed.StartsWith("ADIM"))
            {
                logger.LogDebug("Skipping header/instruction line: {Line}", trimmed);
                continue;
            }
            
            // "- " veya "• " veya numara ile başlayan satırları al
            var pattern = @"^[-•\d\.\s]+\s*(.+)$";
            var match = System.Text.RegularExpressions.Regex.Match(trimmed, pattern);
            
            if (match.Success)
            {
                var ingredient = match.Groups[1].Value.Trim();
                if (!string.IsNullOrEmpty(ingredient))
                {
                    logger.LogDebug("Parsed ingredient: {Ingredient}", ingredient);
                    ingredients.Add(ingredient);
                }
            }
            else
            {
                // Eğer format uymuyorsa ama içerik varsa direkt ekle
                logger.LogDebug("Adding unformatted line as ingredient: {Line}", trimmed);
                ingredients.Add(trimmed);
            }
        }

        logger.LogInformation("Final parsed ingredients count: {Count}", ingredients.Count);
        return ingredients;
    }
}


