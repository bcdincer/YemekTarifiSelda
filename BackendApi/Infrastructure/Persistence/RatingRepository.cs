using BackendApi.Domain.Entities;
using BackendApi.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace BackendApi.Infrastructure.Persistence;

public class RatingRepository(AppDbContext context) : IRatingRepository
{
    public async Task<RecipeRating?> GetByRecipeAndUserAsync(int recipeId, string userId)
    {
        return await context.RecipeRatings
            .FirstOrDefaultAsync(r => r.RecipeId == recipeId && r.UserId == userId);
    }

    public async Task<RecipeRating> AddAsync(RecipeRating rating)
    {
        await context.RecipeRatings.AddAsync(rating);
        return rating;
    }

    public async Task<RecipeRating> UpdateAsync(RecipeRating rating)
    {
        context.RecipeRatings.Update(rating);
        return await Task.FromResult(rating);
    }

    public async Task DeleteAsync(RecipeRating rating)
    {
        context.RecipeRatings.Remove(rating);
        await Task.CompletedTask;
    }

    public async Task<List<RecipeRating>> GetByRecipeIdAsync(int recipeId)
    {
        return await context.RecipeRatings
            .Where(r => r.RecipeId == recipeId)
            .ToListAsync();
    }

    public async Task<double?> GetAverageRatingAsync(int recipeId)
    {
        var ratings = await context.RecipeRatings
            .Where(r => r.RecipeId == recipeId)
            .Select(r => r.Rating)
            .ToListAsync();

        if (!ratings.Any())
            return null;

        return ratings.Average();
    }

    public async Task<int> GetRatingCountAsync(int recipeId)
    {
        return await context.RecipeRatings
            .CountAsync(r => r.RecipeId == recipeId);
    }

    public async Task<Dictionary<int, double?>> GetAverageRatingsAsync(List<int> recipeIds)
    {
        if (!recipeIds.Any())
            return new Dictionary<int, double?>();

        var ratings = await context.RecipeRatings
            .Where(r => recipeIds.Contains(r.RecipeId))
            .GroupBy(r => r.RecipeId)
            .Select(g => new
            {
                RecipeId = g.Key,
                AverageRating = g.Average(r => (double)r.Rating)
            })
            .ToListAsync();

        var result = new Dictionary<int, double?>();
        foreach (var recipeId in recipeIds)
        {
            var rating = ratings.FirstOrDefault(r => r.RecipeId == recipeId);
            result[recipeId] = rating?.AverageRating;
        }

        return result;
    }

    public async Task<Dictionary<int, int>> GetRatingCountsAsync(List<int> recipeIds)
    {
        if (!recipeIds.Any())
            return new Dictionary<int, int>();

        var counts = await context.RecipeRatings
            .Where(r => recipeIds.Contains(r.RecipeId))
            .GroupBy(r => r.RecipeId)
            .Select(g => new
            {
                RecipeId = g.Key,
                Count = g.Count()
            })
            .ToListAsync();

        var result = new Dictionary<int, int>();
        foreach (var recipeId in recipeIds)
        {
            var count = counts.FirstOrDefault(c => c.RecipeId == recipeId);
            result[recipeId] = count?.Count ?? 0;
        }

        return result;
    }

    public async Task SaveChangesAsync()
    {
        await context.SaveChangesAsync();
    }
}

