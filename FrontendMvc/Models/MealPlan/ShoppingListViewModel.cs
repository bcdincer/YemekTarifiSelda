namespace FrontendMvc.Models.MealPlan;

public class ShoppingListViewModel
{
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int? MealPlanId { get; set; }
    public bool IsCompleted { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public List<ShoppingListItemViewModel> Items { get; set; } = new();
}

public class ShoppingListItemViewModel
{
    public int Id { get; set; }
    public string Ingredient { get; set; } = string.Empty;
    public string? Quantity { get; set; }
    public string? Unit { get; set; }
    public bool IsChecked { get; set; }
    public int DisplayOrder { get; set; }
}

public class CreateShoppingListViewModel
{
    public string Name { get; set; } = string.Empty;
    public int? MealPlanId { get; set; }
    public List<CreateShoppingListItemViewModel> Items { get; set; } = new();
}

public class CreateShoppingListItemViewModel
{
    public string Ingredient { get; set; } = string.Empty;
    public string? Quantity { get; set; }
    public string? Unit { get; set; }
}

