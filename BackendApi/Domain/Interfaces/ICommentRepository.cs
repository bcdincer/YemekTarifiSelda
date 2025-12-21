using BackendApi.Domain.Entities;

namespace BackendApi.Domain.Interfaces;

public interface ICommentRepository
{
    Task<RecipeComment?> GetByIdAsync(int id);
    Task<List<RecipeComment>> GetByRecipeIdAsync(int recipeId, int? skip = null, int? take = null);
    Task<List<RecipeComment>> GetByUserIdAsync(string userId);
    Task<int> GetCountByRecipeIdAsync(int recipeId);
    Task<RecipeComment> AddAsync(RecipeComment comment);
    Task UpdateAsync(RecipeComment comment);
    Task DeleteAsync(RecipeComment comment);
    Task SaveChangesAsync();
}

