namespace BackendApi.Application.DTOs;

public class CreateRecipeDto
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    
    // Yeni yapı: Liste olarak malzemeler ve adımlar
    public List<string> Ingredients { get; set; } = new();
    public List<string> Steps { get; set; } = new();
    
    // Backward compatibility için (eski string formatı)
    [Obsolete("Use Ingredients list instead")]
    public string IngredientsString { get; set; } = string.Empty;
    
    [Obsolete("Use Steps list instead")]
    public string StepsString { get; set; } = string.Empty;
    
    public int PrepTimeMinutes { get; set; }
    public int CookingTimeMinutes { get; set; }
    public int Servings { get; set; }
    public string Difficulty { get; set; } = "Orta";
    public string? ImageUrl { get; set; } // Backward compatibility için
    public List<string>? ImageUrls { get; set; } // Çoklu fotoğraf URL'leri
    public int? PrimaryImageIndex { get; set; } // Hangi fotoğraf ana fotoğraf olacak (0-based index)
    public List<int>? RemovedImageIds { get; set; } // Silinecek fotoğraf ID'leri
    public string? Tips { get; set; }
    public string? AlternativeIngredients { get; set; }
    public string? NutritionInfo { get; set; }
    public int? CategoryId { get; set; }
    public int? AuthorId { get; set; }
    public bool IsFeatured { get; set; }
}

