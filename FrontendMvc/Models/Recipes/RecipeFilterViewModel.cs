namespace FrontendMvc.Models.Recipes;

public class RecipeFilterViewModel
{
    public string? SearchTerm { get; set; }
    public int? CategoryId { get; set; }
    public string? Difficulty { get; set; } // "Kolay", "Orta", "Zor"
    public int? MaxPrepTime { get; set; }
    public int? MaxCookingTime { get; set; }
    public int? MaxTotalTime { get; set; }
    public int? MinServings { get; set; }
    public int? MaxServings { get; set; }
    public string? Ingredient { get; set; }
    public bool? IsFeatured { get; set; }
    public double? MinRating { get; set; }
    public int? MinRatingCount { get; set; }
    public string? SortBy { get; set; }
    public bool SortDescending { get; set; } = true;
}

