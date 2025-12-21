using BackendApi.Application.DTOs;
using BackendApi.Domain.Entities;

namespace BackendApi.Application.Mappings;

public static class RecipeMapper
{
    public static Recipe ToEntity(this CreateRecipeDto dto)
    {
        return new Recipe
        {
            Title = dto.Title,
            Description = dto.Description,
            Ingredients = dto.Ingredients,
            Steps = dto.Steps,
            PrepTimeMinutes = dto.PrepTimeMinutes,
            CookingTimeMinutes = dto.CookingTimeMinutes,
            Servings = dto.Servings,
            Difficulty = Enum.TryParse<DifficultyLevel>(dto.Difficulty, true, out var difficulty)
                ? difficulty
                : DifficultyLevel.Orta,
            ImageUrl = dto.ImageUrl,
            Tips = dto.Tips,
            AlternativeIngredients = dto.AlternativeIngredients,
            NutritionInfo = dto.NutritionInfo,
            CategoryId = dto.CategoryId,
            IsFeatured = dto.IsFeatured
        };
    }

    public static void UpdateEntity(this Recipe existing, CreateRecipeDto dto)
    {
        existing.Title = dto.Title;
        existing.Description = dto.Description;
        existing.Ingredients = dto.Ingredients;
        existing.Steps = dto.Steps;
        existing.PrepTimeMinutes = dto.PrepTimeMinutes;
        existing.CookingTimeMinutes = dto.CookingTimeMinutes;
        existing.Servings = dto.Servings;
        existing.Difficulty = Enum.TryParse<DifficultyLevel>(dto.Difficulty, true, out var difficulty)
            ? difficulty
            : DifficultyLevel.Orta;
        existing.ImageUrl = dto.ImageUrl;
        existing.Tips = dto.Tips;
        existing.AlternativeIngredients = dto.AlternativeIngredients;
        existing.NutritionInfo = dto.NutritionInfo;
        existing.CategoryId = dto.CategoryId;
        existing.IsFeatured = dto.IsFeatured;
        existing.UpdatedAt = DateTime.UtcNow;
    }

    public static RecipeResponseDto ToDto(this Recipe recipe)
    {
        return new RecipeResponseDto
        {
            Id = recipe.Id,
            Title = recipe.Title,
            Description = recipe.Description,
            Ingredients = recipe.Ingredients,
            Steps = recipe.Steps,
            PrepTimeMinutes = recipe.PrepTimeMinutes,
            CookingTimeMinutes = recipe.CookingTimeMinutes,
            Servings = recipe.Servings,
            Difficulty = recipe.Difficulty.ToString(),
            ImageUrl = recipe.ImageUrl,
            Tips = recipe.Tips,
            AlternativeIngredients = recipe.AlternativeIngredients,
            NutritionInfo = recipe.NutritionInfo,
            ViewCount = recipe.ViewCount,
            AverageRating = recipe.AverageRating,
            RatingCount = recipe.RatingCount,
            LikeCount = recipe.LikeCount,
            IsFeatured = recipe.IsFeatured,
            CategoryId = recipe.CategoryId,
            Category = recipe.Category != null ? new CategoryDto
            {
                Id = recipe.Category.Id,
                Name = recipe.Category.Name,
                Description = recipe.Category.Description,
                Icon = recipe.Category.Icon
            } : null,
            CreatedAt = recipe.CreatedAt,
            UpdatedAt = recipe.UpdatedAt
        };
    }
}

