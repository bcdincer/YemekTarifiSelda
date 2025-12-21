using BackendApi.Domain.Entities;
using BackendApi.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace BackendApi.Infrastructure.Persistence;

public class LikeRepository(AppDbContext context) : ILikeRepository
{
    public async Task<bool> ExistsAsync(int recipeId, string userId)
    {
        return await context.RecipeLikes
            .AnyAsync(l => l.RecipeId == recipeId && l.UserId == userId);
    }

    public async Task<RecipeLike> AddAsync(RecipeLike like)
    {
        await context.RecipeLikes.AddAsync(like);
        return like;
    }

    public async Task DeleteAsync(RecipeLike like)
    {
        context.RecipeLikes.Remove(like);
        await Task.CompletedTask;
    }

    public async Task<RecipeLike?> GetByRecipeAndUserAsync(int recipeId, string userId)
    {
        return await context.RecipeLikes
            .FirstOrDefaultAsync(l => l.RecipeId == recipeId && l.UserId == userId);
    }

    public async Task<int> GetLikeCountAsync(int recipeId)
    {
        return await context.RecipeLikes
            .CountAsync(l => l.RecipeId == recipeId);
    }

    public async Task<Dictionary<int, int>> GetLikeCountsAsync(List<int> recipeIds)
    {
        if (!recipeIds.Any())
            return new Dictionary<int, int>();

        var counts = await context.RecipeLikes
            .Where(l => recipeIds.Contains(l.RecipeId))
            .GroupBy(l => l.RecipeId)
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

    public async Task<List<RecipeLike>> GetByRecipeIdAsync(int recipeId)
    {
        return await context.RecipeLikes
            .Where(l => l.RecipeId == recipeId)
            .ToListAsync();
    }

    public async Task<List<RecipeLike>> GetByUserIdAsync(string userId)
    {
        return await context.RecipeLikes
            .Include(l => l.Recipe)
                .ThenInclude(r => r!.Category)
            .Where(l => l.UserId == userId)
            .OrderByDescending(l => l.CreatedAt)
            .ToListAsync();
    }

    public async Task SaveChangesAsync()
    {
        await context.SaveChangesAsync();
    }
}

