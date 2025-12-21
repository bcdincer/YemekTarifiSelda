namespace BackendApi.Application.Services;

public interface ILikeService
{
    Task ToggleLikeAsync(int recipeId, string userId);
    Task<bool> IsLikedAsync(int recipeId, string userId);
    Task<int> GetLikeCountAsync(int recipeId);
    Task<Dictionary<int, int>> GetLikeCountsAsync(List<int> recipeIds);
    Task<List<int>> GetLikedRecipeIdsAsync(string userId);
}

