namespace BackendApi.Domain.Entities;

public class RecipeIngredient
{
    public int Id { get; set; }
    public int RecipeId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Order { get; set; } // Sıralama için
    
    // Navigation property
    public Recipe Recipe { get; set; } = null!;
}

