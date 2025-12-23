namespace BackendApi.Domain.Entities;

public class RecipeImage
{
    public int Id { get; set; }
    public int RecipeId { get; set; }
    public Recipe Recipe { get; set; } = null!;
    public string ImageUrl { get; set; } = string.Empty;
    public bool IsPrimary { get; set; } = false; // Ana fotoğraf mı?
    public int DisplayOrder { get; set; } = 0; // Sıralama
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

