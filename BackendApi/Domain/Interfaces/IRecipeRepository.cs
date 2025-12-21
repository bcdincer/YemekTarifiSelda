using BackendApi.Domain.Entities;

namespace BackendApi.Domain.Interfaces;

public interface IRecipeRepository
{
    Task<List<Recipe>> GetAllAsync();
    Task<(List<Recipe> Items, int TotalCount)> GetAllPagedAsync(int pageNumber, int pageSize);
    Task<Recipe?> GetByIdAsync(int id);
    Task<List<Recipe>> GetFeaturedAsync(int count = 6);
    Task<List<Recipe>> GetPopularAsync(int count = 6);
    Task<List<Recipe>> GetByCategoryAsync(int categoryId);
    Task<(List<Recipe> Items, int TotalCount)> GetByCategoryPagedAsync(int categoryId, int pageNumber, int pageSize);
    Task<List<Recipe>> SearchAsync(string searchTerm);
    Task<(List<Recipe> Items, int TotalCount)> SearchPagedAsync(string searchTerm, int pageNumber, int pageSize);
    Task AddAsync(Recipe recipe);
    Task UpdateAsync(Recipe recipe);
    Task DeleteAsync(Recipe recipe);
    Task SaveChangesAsync();
}


