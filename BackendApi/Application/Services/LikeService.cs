using BackendApi.Domain.Entities;
using BackendApi.Domain.Interfaces;

namespace BackendApi.Application.Services;

public class LikeService(
    IUnitOfWork unitOfWork,
    ILogger<LikeService> logger) : ILikeService
{
    private ILikeRepository LikeRepository => unitOfWork.Likes;
    private IRecipeRepository RecipeRepository => unitOfWork.Recipes;

    public async Task ToggleLikeAsync(int recipeId, string userId)
    {
        try
        {
            // Transaction başlat - hem like hem de recipe update atomic olmalı
            await unitOfWork.BeginTransactionAsync();

            var existingLike = await LikeRepository.GetByRecipeAndUserAsync(recipeId, userId);

            if (existingLike != null)
            {
                // Beğeniyi kaldır
                await LikeRepository.DeleteAsync(existingLike);
            }
            else
            {
                // Beğeni ekle
                var newLike = new RecipeLike
                {
                    RecipeId = recipeId,
                    UserId = userId,
                    CreatedAt = DateTime.UtcNow
                };
                await LikeRepository.AddAsync(newLike);
            }

            // Recipe'nin like count'unu güncelle
            var likeCount = await LikeRepository.GetLikeCountAsync(recipeId);
            var recipe = await RecipeRepository.GetByIdAsync(recipeId);
            if (recipe != null)
            {
                recipe.LikeCount = likeCount;
                await RecipeRepository.UpdateAsync(recipe);
            }

            // Tüm değişiklikleri tek transaction'da commit et
            await unitOfWork.CommitTransactionAsync();
            logger.LogInformation("Like toggled for recipe {RecipeId} by user {UserId}", recipeId, userId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error toggling like for recipe {RecipeId} by user {UserId}", recipeId, userId);
            await unitOfWork.RollbackTransactionAsync();
            throw;
        }
    }

    public async Task<bool> IsLikedAsync(int recipeId, string userId)
    {
        return await LikeRepository.ExistsAsync(recipeId, userId);
    }

    public async Task<int> GetLikeCountAsync(int recipeId)
    {
        return await LikeRepository.GetLikeCountAsync(recipeId);
    }

    public async Task<Dictionary<int, int>> GetLikeCountsAsync(List<int> recipeIds)
    {
        return await LikeRepository.GetLikeCountsAsync(recipeIds);
    }

    public async Task<List<int>> GetLikedRecipeIdsAsync(string userId)
    {
        var likes = await LikeRepository.GetByUserIdAsync(userId);
        return likes.Select(l => l.RecipeId).ToList();
    }
}

