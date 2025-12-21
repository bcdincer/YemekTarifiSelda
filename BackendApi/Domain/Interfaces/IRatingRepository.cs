using BackendApi.Domain.Entities;

namespace BackendApi.Domain.Interfaces;

public interface IRatingRepository
{
    Task<RecipeRating?> GetByRecipeAndUserAsync(int recipeId, string userId);
    Task<RecipeRating> AddAsync(RecipeRating rating);
    Task<RecipeRating> UpdateAsync(RecipeRating rating);
    Task DeleteAsync(RecipeRating rating);
    Task<List<RecipeRating>> GetByRecipeIdAsync(int recipeId);
    Task<double?> GetAverageRatingAsync(int recipeId);
    Task<int> GetRatingCountAsync(int recipeId);
    Task<Dictionary<int, double?>> GetAverageRatingsAsync(List<int> recipeIds);
    Task<Dictionary<int, int>> GetRatingCountsAsync(List<int> recipeIds);
    Task SaveChangesAsync();
}

