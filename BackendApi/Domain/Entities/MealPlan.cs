namespace BackendApi.Domain.Entities;

public class MealPlan
{
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty; // Plan adı (örn: "Aralık 2024 - Hafta 1")
    public DateTime StartDate { get; set; } // Plan başlangıç tarihi
    public DateTime EndDate { get; set; } // Plan bitiş tarihi
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    
    // Navigation properties
    public List<MealPlanItem> Items { get; set; } = new();
}

