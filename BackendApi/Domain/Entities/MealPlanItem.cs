namespace BackendApi.Domain.Entities;

public enum MealType
{
    Kahvaltı = 1,
    ÖğleYemeği = 2,
    AkşamYemeği = 3,
    Atıştırmalık = 4
}

public class MealPlanItem
{
    public int Id { get; set; }
    public int MealPlanId { get; set; }
    public int RecipeId { get; set; }
    public DateTime Date { get; set; } // Hangi gün için
    public MealType MealType { get; set; } // Kahvaltı, Öğle, Akşam, Atıştırmalık
    public int Servings { get; set; } = 4; // Kaç kişilik
    public int DisplayOrder { get; set; } = 0; // Gün içinde sıralama
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    // Navigation properties
    public MealPlan MealPlan { get; set; } = null!;
    public Recipe Recipe { get; set; } = null!;
}

