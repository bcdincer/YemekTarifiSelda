namespace BackendApi.Application.Services;

public interface IRatingService
{
    Task RateRecipeAsync(int recipeId, string userId, int rating);
    Task<double?> GetAverageRatingAsync(int recipeId);
    Task<int> GetRatingCountAsync(int recipeId);
    Task<Dictionary<int, double?>> GetAverageRatingsAsync(List<int> recipeIds);
    Task<Dictionary<int, int>> GetRatingCountsAsync(List<int> recipeIds);
    Task<int?> GetUserRatingAsync(int recipeId, string userId);
}

