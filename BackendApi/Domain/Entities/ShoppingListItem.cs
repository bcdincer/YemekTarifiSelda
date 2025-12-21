namespace BackendApi.Domain.Entities;

public class ShoppingListItem
{
    public int Id { get; set; }
    public int ShoppingListId { get; set; }
    public string Ingredient { get; set; } = string.Empty; // Malzeme adı
    public string? Quantity { get; set; } // Miktar (örn: "2", "500g", "1 kg")
    public string? Unit { get; set; } // Birim (opsiyonel)
    public bool IsChecked { get; set; } = false; // Alışverişte işaretlendi mi?
    public int DisplayOrder { get; set; } = 0; // Sıralama
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    // Navigation properties
    public ShoppingList ShoppingList { get; set; } = null!;
}

