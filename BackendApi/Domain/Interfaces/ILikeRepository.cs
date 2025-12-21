using BackendApi.Domain.Entities;

namespace BackendApi.Domain.Interfaces;

public interface ILikeRepository
{
    Task<bool> ExistsAsync(int recipeId, string userId);
    Task<RecipeLike> AddAsync(RecipeLike like);
    Task DeleteAsync(RecipeLike like);
    Task<RecipeLike?> GetByRecipeAndUserAsync(int recipeId, string userId);
    Task<int> GetLikeCountAsync(int recipeId);
    Task<Dictionary<int, int>> GetLikeCountsAsync(List<int> recipeIds);
    Task<List<RecipeLike>> GetByRecipeIdAsync(int recipeId);
    Task<List<RecipeLike>> GetByUserIdAsync(string userId);
    Task SaveChangesAsync();
}

