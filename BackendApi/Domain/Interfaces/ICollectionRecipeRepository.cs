using BackendApi.Domain.Entities;

namespace BackendApi.Domain.Interfaces;

public interface ICollectionRecipeRepository
{
    Task<CollectionRecipe?> GetByIdAsync(int id);
    Task<CollectionRecipe?> GetByCollectionAndRecipeAsync(int collectionId, int recipeId);
    Task<List<CollectionRecipe>> GetByCollectionIdAsync(int collectionId);
    Task<List<CollectionRecipe>> GetByRecipeIdAsync(int recipeId);
    Task<List<int>> GetRecipeIdsByCollectionIdAsync(int collectionId);
    Task<CollectionRecipe> AddAsync(CollectionRecipe collectionRecipe);
    Task DeleteAsync(CollectionRecipe collectionRecipe);
    Task<bool> ExistsAsync(int collectionId, int recipeId);
    Task SaveChangesAsync();
}

