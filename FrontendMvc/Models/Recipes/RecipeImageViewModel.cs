namespace FrontendMvc.Models.Recipes;

public class RecipeImageViewModel
{
    public int Id { get; set; }
    public int RecipeId { get; set; }
    public string ImageUrl { get; set; } = string.Empty;
    public bool IsPrimary { get; set; } = false;
    public int DisplayOrder { get; set; } = 0;
    public DateTime CreatedAt { get; set; }
}

