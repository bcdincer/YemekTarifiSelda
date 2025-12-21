using BackendApi.Domain.Entities;

namespace BackendApi.Domain.Interfaces;

/// <summary>
/// AI Agent service interface for AI-powered features
/// </summary>
public interface IAiAgentService : IExternalService
{
    /// <summary>
    /// Analyzes recipe content and suggests improvements
    /// </summary>
    Task<RecipeAnalysisResult> AnalyzeRecipeAsync(int recipeId, string recipeContent);

    /// <summary>
    /// Generates recipe suggestions based on user preferences
    /// </summary>
    Task<List<RecipeSuggestion>> GetRecipeSuggestionsAsync(string userId, UserPreferences preferences);

    /// <summary>
    /// Generates recipe description or content using AI
    /// </summary>
    Task<string> GenerateRecipeContentAsync(string prompt, RecipeContentType contentType);

    /// <summary>
    /// Validates recipe content using AI
    /// </summary>
    Task<RecipeValidationResult> ValidateRecipeAsync(string recipeContent);
}

public class RecipeAnalysisResult
{
    public float Score { get; set; }
    public List<string> Suggestions { get; set; } = new();
    public List<string> Warnings { get; set; } = new();
    public Dictionary<string, object> Metadata { get; set; } = new();
}

public class RecipeSuggestion
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public float RelevanceScore { get; set; }
    public List<string> Tags { get; set; } = new();
}

public class UserPreferences
{
    public List<string> PreferredCategories { get; set; } = new();
    public List<string> Allergies { get; set; } = new();
    public int MaxCookingTime { get; set; }
    public DifficultyLevel PreferredDifficulty { get; set; }
}

public class RecipeValidationResult
{
    public bool IsValid { get; set; }
    public List<string> Errors { get; set; } = new();
    public List<string> Warnings { get; set; } = new();
}

public enum RecipeContentType
{
    Description,
    Ingredients,
    Steps,
    Tips,
    Title
}

public enum DifficultyLevel
{
    Kolay = 1,
    Orta = 2,
    Zor = 3
}

