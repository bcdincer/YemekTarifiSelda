namespace BackendApi.Application.DTOs;

public class CreateMealPlanDto
{
    public string Name { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public List<CreateMealPlanItemDto> Items { get; set; } = new();
}

public class CreateMealPlanItemDto
{
    public int RecipeId { get; set; }
    public DateTime Date { get; set; }
    public string MealType { get; set; } = "AkşamYemeği"; // "Kahvaltı", "ÖğleYemeği", "AkşamYemeği", "Atıştırmalık"
    public int Servings { get; set; } = 4;
}

public class MealPlanResponseDto
{
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public List<MealPlanItemResponseDto> Items { get; set; } = new();
}

public class MealPlanItemResponseDto
{
    public int Id { get; set; }
    public int RecipeId { get; set; }
    public RecipeResponseDto? Recipe { get; set; }
    public DateTime Date { get; set; }
    public string MealType { get; set; } = string.Empty;
    public int Servings { get; set; }
    public int DisplayOrder { get; set; }
}

