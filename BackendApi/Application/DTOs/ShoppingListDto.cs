namespace BackendApi.Application.DTOs;

public class CreateShoppingListDto
{
    public string Name { get; set; } = string.Empty;
    public int? MealPlanId { get; set; }
    public List<CreateShoppingListItemDto> Items { get; set; } = new();
}

public class CreateShoppingListItemDto
{
    public string Ingredient { get; set; } = string.Empty;
    public string? Quantity { get; set; }
    public string? Unit { get; set; }
}

public class ShoppingListResponseDto
{
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int? MealPlanId { get; set; }
    public bool IsCompleted { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public List<ShoppingListItemResponseDto> Items { get; set; } = new();
}

public class ShoppingListItemResponseDto
{
    public int Id { get; set; }
    public string Ingredient { get; set; } = string.Empty;
    public string? Quantity { get; set; }
    public string? Unit { get; set; }
    public bool IsChecked { get; set; }
    public int DisplayOrder { get; set; }
}

