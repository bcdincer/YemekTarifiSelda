using BackendApi.Domain.Entities;

namespace BackendApi.Domain.Interfaces;

public interface IRecipeImageRepository
{
    Task<List<RecipeImage>> GetByRecipeIdAsync(int recipeId);
    Task<RecipeImage?> GetByIdAsync(int id);
    Task<RecipeImage> AddAsync(RecipeImage image);
    Task DeleteAsync(RecipeImage image);
    Task DeleteRangeAsync(IEnumerable<RecipeImage> images);
    Task UpdateAsync(RecipeImage image);
}

