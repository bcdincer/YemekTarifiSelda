namespace BackendApi.Domain.Entities;

public class Category
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Icon { get; set; } // İkon class adı (örn: "ti-book")
    public int DisplayOrder { get; set; } // Menüde sıralama
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    // Navigation property
    public List<Recipe> Recipes { get; set; } = new();
}

