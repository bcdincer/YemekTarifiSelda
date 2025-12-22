namespace BackendApi.Domain.Entities;

public class RecipeStep
{
    public int Id { get; set; }
    public int RecipeId { get; set; }
    public string Description { get; set; } = string.Empty;
    public int Order { get; set; } // Sıralama için
    
    // Navigation property
    public Recipe Recipe { get; set; } = null!;
}

