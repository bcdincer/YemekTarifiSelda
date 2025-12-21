using BackendApi.Application.DTOs;
using BackendApi.Domain.Entities;

namespace BackendApi.Application.Mappings;

public static class MealPlanMapper
{
    public static MealPlanResponseDto ToDto(this MealPlan mealPlan)
    {
        return new MealPlanResponseDto
        {
            Id = mealPlan.Id,
            UserId = mealPlan.UserId,
            Name = mealPlan.Name,
            StartDate = mealPlan.StartDate,
            EndDate = mealPlan.EndDate,
            CreatedAt = mealPlan.CreatedAt,
            UpdatedAt = mealPlan.UpdatedAt,
            Items = mealPlan.Items.Select(i => i.ToDto()).ToList()
        };
    }

    public static MealPlanItemResponseDto ToDto(this MealPlanItem item)
    {
        return new MealPlanItemResponseDto
        {
            Id = item.Id,
            RecipeId = item.RecipeId,
            Recipe = item.Recipe?.ToDto(),
            Date = item.Date,
            MealType = item.MealType.ToString(),
            Servings = item.Servings,
            DisplayOrder = item.DisplayOrder
        };
    }
}

