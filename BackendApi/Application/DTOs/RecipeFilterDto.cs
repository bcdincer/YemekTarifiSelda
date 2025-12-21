using BackendApi.Domain.Entities;

namespace BackendApi.Application.DTOs;

public class RecipeFilterDto
{
    public string? SearchTerm { get; set; } // Arama terimi
    public int? CategoryId { get; set; } // Kategori filtresi
    public DifficultyLevel? Difficulty { get; set; } // Zorluk seviyesi (Kolay, Orta, Zor)
    public int? MaxPrepTime { get; set; } // Maksimum hazırlık süresi (dakika)
    public int? MaxCookingTime { get; set; } // Maksimum pişirme süresi (dakika)
    public int? MaxTotalTime { get; set; } // Maksimum toplam süre (hazırlık + pişirme)
    public int? MinServings { get; set; } // Minimum porsiyon sayısı
    public int? MaxServings { get; set; } // Maksimum porsiyon sayısı
    public string? Ingredient { get; set; } // Malzeme filtresi (içinde geçen malzeme)
    public List<string>? Ingredients { get; set; } // Birden fazla malzeme filtresi (hepsi içinde olmalı)
    public List<string>? ExcludedIngredients { get; set; } // Hariç tutulacak malzemeler (alerjenler)
    public string? DietType { get; set; } // Diyet tipi: "vegan", "vegetarian", "gluten-free", "dairy-free", "nut-free"
    public bool? IsFeatured { get; set; } // Öne çıkan tarifler
    public double? MinRating { get; set; } // Minimum puan (1-5)
    public int? MinRatingCount { get; set; } // Minimum puan sayısı
    public string? SortBy { get; set; } // Sıralama: "newest", "rating", "likes", "views", "prepTime", "cookingTime"
    public bool SortDescending { get; set; } = true; // Azalan sıralama (default: true)
}

