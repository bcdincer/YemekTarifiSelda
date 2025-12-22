using BackendApi.Application.DTOs;
using BackendApi.Domain.Entities;

namespace BackendApi.Application.Services;

public interface IRecipeService
{
    Task<List<RecipeResponseDto>> GetAllAsync();
    Task<PagedResult<RecipeResponseDto>> GetAllPagedAsync(int pageNumber, int pageSize);
    Task<RecipeResponseDto?> GetByIdAsync(int id);
    Task<List<RecipeResponseDto>> GetFeaturedAsync(int count = 6);
    Task<List<RecipeResponseDto>> GetPopularAsync(int count = 6);
    Task<RecipeResponseDto?> GetRandomAsync();
    Task<List<RecipeResponseDto>> GetByCategoryAsync(int categoryId);
    Task<PagedResult<RecipeResponseDto>> GetByCategoryPagedAsync(int categoryId, int pageNumber, int pageSize);
    Task<List<RecipeResponseDto>> SearchAsync(string searchTerm);
    Task<PagedResult<RecipeResponseDto>> SearchPagedAsync(string searchTerm, int pageNumber, int pageSize);
    Task<PagedResult<RecipeResponseDto>> SearchWithFiltersAsync(RecipeFilterDto filter, int pageNumber, int pageSize);
    Task<RecipeResponseDto> CreateAsync(Recipe recipe);
    Task<bool> UpdateAsync(int id, Recipe updated);
    Task<bool> DeleteAsync(int id);
    Task IncrementViewCountAsync(int id);
}


