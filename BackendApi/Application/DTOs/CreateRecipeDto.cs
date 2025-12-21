namespace BackendApi.Application.DTOs;

public class CreateRecipeDto
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Ingredients { get; set; } = string.Empty;
    public string Steps { get; set; } = string.Empty;
    public int PrepTimeMinutes { get; set; }
    public int CookingTimeMinutes { get; set; }
    public int Servings { get; set; }
    public string Difficulty { get; set; } = "Orta";
    public string? ImageUrl { get; set; }
    public string? Tips { get; set; }
    public string? AlternativeIngredients { get; set; }
    public string? NutritionInfo { get; set; }
    public int? CategoryId { get; set; }
    public bool IsFeatured { get; set; }
}

