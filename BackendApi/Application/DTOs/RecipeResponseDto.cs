namespace BackendApi.Application.DTOs;

public class RecipeResponseDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Ingredients { get; set; } = string.Empty;
    public string Steps { get; set; } = string.Empty;
    public int PrepTimeMinutes { get; set; }
    public int CookingTimeMinutes { get; set; }
    public int Servings { get; set; }
    public string Difficulty { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
    public string? Tips { get; set; }
    public string? AlternativeIngredients { get; set; }
    public string? NutritionInfo { get; set; }
    public int ViewCount { get; set; }
    public double? AverageRating { get; set; }
    public int RatingCount { get; set; }
    public int LikeCount { get; set; }
    public bool IsFeatured { get; set; }
    public int? CategoryId { get; set; }
    public CategoryDto? Category { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class CategoryDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Icon { get; set; }
}

