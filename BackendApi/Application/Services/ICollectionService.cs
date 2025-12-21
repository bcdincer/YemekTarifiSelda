using BackendApi.Application.DTOs;

namespace BackendApi.Application.Services;

public interface ICollectionService
{
    Task<CollectionResponseDto> CreateAsync(CreateCollectionDto dto, string userId);
    Task<CollectionResponseDto?> GetByIdAsync(int id, string userId);
    Task<List<CollectionResponseDto>> GetByUserIdAsync(string userId);
    Task<CollectionDetailDto?> GetDetailByIdAsync(int id, string userId);
    Task<CollectionResponseDto> UpdateAsync(int id, UpdateCollectionDto dto, string userId);
    Task DeleteAsync(int id, string userId);
    Task AddRecipeToCollectionAsync(int collectionId, int recipeId, string userId);
    Task RemoveRecipeFromCollectionAsync(int collectionId, int recipeId, string userId);
    Task ToggleRecipeInCollectionAsync(int collectionId, int recipeId, string userId);
    Task<List<int>> GetCollectionIdsForRecipeAsync(int recipeId, string userId);
    Task<List<int>> GetCollectionsForRecipeAsync(int recipeId, string userId);
}

