namespace BackendApi.Domain.Entities;

public class ShoppingList
{
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty; // Liste adı
    public int? MealPlanId { get; set; } // Hangi meal plan'tan oluşturuldu (opsiyonel)
    public bool IsCompleted { get; set; } = false; // Tamamlandı mı?
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    
    // Navigation properties
    public MealPlan? MealPlan { get; set; }
    public List<ShoppingListItem> Items { get; set; } = new();
}

