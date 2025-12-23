namespace BackendApi.Domain.Entities;

public enum DifficultyLevel
{
    Kolay = 1,
    Orta = 2,
    Zor = 3
}

public class Recipe
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    
    // Malzemeler ve adımlar - 1-N ilişki
    public ICollection<RecipeIngredient> Ingredients { get; set; } = new List<RecipeIngredient>();
    public ICollection<RecipeStep> Steps { get; set; } = new List<RecipeStep>();
    
    // Backward compatibility için (eski veriler için)
    [Obsolete("Use Ingredients collection instead")]
    public string IngredientsString { get; set; } = string.Empty;
    
    [Obsolete("Use Steps collection instead")]
    public string StepsString { get; set; } = string.Empty;
    
    // Süre bilgileri
    public int PrepTimeMinutes { get; set; } // Hazırlık süresi
    public int CookingTimeMinutes { get; set; } // Pişirme süresi
    
    // Diğer bilgiler
    public int Servings { get; set; } = 4; // Kaç kişilik
    public DifficultyLevel Difficulty { get; set; } = DifficultyLevel.Orta;
    public string? ImageUrl { get; set; } // Yemek görseli URL'i
    
    // Ekstra bilgiler
    public string? Tips { get; set; } // Püf noktaları
    public string? AlternativeIngredients { get; set; } // Alternatif malzemeler
    public string? NutritionInfo { get; set; } // Besin değerleri (JSON)
    
    // İstatistikler
    public int ViewCount { get; set; } = 0; // Kaç kez görüntülendi
    public double? AverageRating { get; set; } // Ortalama puan (1-5)
    public int RatingCount { get; set; } = 0; // Kaç kişi puanladı
    public int LikeCount { get; set; } = 0; // Kaç kişi beğendi
    public bool IsFeatured { get; set; } = false; // Öne çıkan tarif mi?
    
    // İlişkiler
    public int? CategoryId { get; set; }
    public Category? Category { get; set; }
    
    public int? AuthorId { get; set; }
    public Author? Author { get; set; }
    
    // Çoklu fotoğraflar
    public ICollection<RecipeImage> Images { get; set; } = new List<RecipeImage>();
    
    // Tarih bilgileri
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}


