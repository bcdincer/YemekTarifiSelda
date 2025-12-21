using BackendApi.Domain.Entities;
using BackendApi.Domain.Interfaces;

namespace BackendApi.Application.Services;

public class RatingService(
    IUnitOfWork unitOfWork,
    ILogger<RatingService> logger) : IRatingService
{
    private IRatingRepository RatingRepository => unitOfWork.Ratings;
    private IRecipeRepository RecipeRepository => unitOfWork.Recipes;

    public async Task RateRecipeAsync(int recipeId, string userId, int rating)
    {
        // Rating 1-5 arası olmalı
        if (rating < 1 || rating > 5)
            throw new ArgumentException("Rating must be between 1 and 5", nameof(rating));

        try
        {
            // Transaction başlat - hem rating hem de recipe update atomic olmalı
            await unitOfWork.BeginTransactionAsync();

            var existingRating = await RatingRepository.GetByRecipeAndUserAsync(recipeId, userId);

            if (existingRating != null)
            {
                // Mevcut puanı güncelle
                existingRating.Rating = rating;
                existingRating.UpdatedAt = DateTime.UtcNow;
                await RatingRepository.UpdateAsync(existingRating);
            }
            else
            {
                // Yeni puan ekle
                var newRating = new RecipeRating
                {
                    RecipeId = recipeId,
                    UserId = userId,
                    Rating = rating,
                    CreatedAt = DateTime.UtcNow
                };
                await RatingRepository.AddAsync(newRating);
            }

            // Recipe'nin ortalama puanını güncelle
            var averageRating = await RatingRepository.GetAverageRatingAsync(recipeId);
            var ratingCount = await RatingRepository.GetRatingCountAsync(recipeId);

            var recipe = await RecipeRepository.GetByIdAsync(recipeId);
            if (recipe != null)
            {
                recipe.AverageRating = averageRating;
                recipe.RatingCount = ratingCount;
                await RecipeRepository.UpdateAsync(recipe);
            }

            // Tüm değişiklikleri tek transaction'da commit et
            await unitOfWork.CommitTransactionAsync();
            logger.LogInformation("Recipe {RecipeId} rated {Rating} by user {UserId}", recipeId, rating, userId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error rating recipe {RecipeId} by user {UserId}", recipeId, userId);
            await unitOfWork.RollbackTransactionAsync();
            throw;
        }
    }

    public async Task<double?> GetAverageRatingAsync(int recipeId)
    {
        return await RatingRepository.GetAverageRatingAsync(recipeId);
    }

    public async Task<int> GetRatingCountAsync(int recipeId)
    {
        return await RatingRepository.GetRatingCountAsync(recipeId);
    }

    public async Task<Dictionary<int, double?>> GetAverageRatingsAsync(List<int> recipeIds)
    {
        return await RatingRepository.GetAverageRatingsAsync(recipeIds);
    }

    public async Task<Dictionary<int, int>> GetRatingCountsAsync(List<int> recipeIds)
    {
        return await RatingRepository.GetRatingCountsAsync(recipeIds);
    }

    public async Task<int?> GetUserRatingAsync(int recipeId, string userId)
    {
        var rating = await RatingRepository.GetByRecipeAndUserAsync(recipeId, userId);
        return rating?.Rating;
    }
}

