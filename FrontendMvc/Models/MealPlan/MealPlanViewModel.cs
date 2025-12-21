using FrontendMvc.Models.Recipes;

namespace FrontendMvc.Models.MealPlan;

public class MealPlanViewModel
{
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public List<MealPlanItemViewModel> Items { get; set; } = new();
}

public class MealPlanItemViewModel
{
    public int Id { get; set; }
    public int RecipeId { get; set; }
    public RecipeViewModel? Recipe { get; set; }
    public DateTime Date { get; set; }
    public string MealType { get; set; } = string.Empty; // "Kahvaltı", "ÖğleYemeği", "AkşamYemeği", "Atıştırmalık"
    public int Servings { get; set; }
    public int DisplayOrder { get; set; }
}

public class CreateMealPlanViewModel
{
    public string Name { get; set; } = string.Empty;
    public DateTime StartDate { get; set; } = DateTime.Now.Date;
    public DateTime EndDate { get; set; } = DateTime.Now.Date.AddDays(6);
    public List<CreateMealPlanItemViewModel> Items { get; set; } = new();
}

public class CreateMealPlanItemViewModel
{
    public int RecipeId { get; set; }
    public DateTime Date { get; set; }
    public string MealType { get; set; } = "AkşamYemeği";
    public int Servings { get; set; } = 4;
}

