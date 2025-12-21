using BackendApi.Domain.Entities;
using BackendApi.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace BackendApi.Infrastructure.Persistence;

public class CollectionRecipeRepository(AppDbContext context) : ICollectionRecipeRepository
{
    public async Task<CollectionRecipe?> GetByIdAsync(int id)
    {
        return await context.CollectionRecipes
            .Include(cr => cr.Collection)
            .Include(cr => cr.Recipe)
            .FirstOrDefaultAsync(cr => cr.Id == id);
    }

    public async Task<CollectionRecipe?> GetByCollectionAndRecipeAsync(int collectionId, int recipeId)
    {
        return await context.CollectionRecipes
            .FirstOrDefaultAsync(cr => cr.CollectionId == collectionId && cr.RecipeId == recipeId);
    }

    public async Task<List<CollectionRecipe>> GetByCollectionIdAsync(int collectionId)
    {
        return await context.CollectionRecipes
            .Include(cr => cr.Recipe)
                .ThenInclude(r => r!.Category)
            .Where(cr => cr.CollectionId == collectionId)
            .OrderByDescending(cr => cr.AddedAt)
            .ToListAsync();
    }

    public async Task<List<CollectionRecipe>> GetByRecipeIdAsync(int recipeId)
    {
        return await context.CollectionRecipes
            .Include(cr => cr.Collection)
            .Where(cr => cr.RecipeId == recipeId)
            .ToListAsync();
    }

    public async Task<List<int>> GetRecipeIdsByCollectionIdAsync(int collectionId)
    {
        return await context.CollectionRecipes
            .Where(cr => cr.CollectionId == collectionId)
            .Select(cr => cr.RecipeId)
            .ToListAsync();
    }

    public async Task<CollectionRecipe> AddAsync(CollectionRecipe collectionRecipe)
    {
        await context.CollectionRecipes.AddAsync(collectionRecipe);
        return collectionRecipe;
    }

    public async Task DeleteAsync(CollectionRecipe collectionRecipe)
    {
        context.CollectionRecipes.Remove(collectionRecipe);
        await Task.CompletedTask;
    }

    public async Task<bool> ExistsAsync(int collectionId, int recipeId)
    {
        return await context.CollectionRecipes
            .AnyAsync(cr => cr.CollectionId == collectionId && cr.RecipeId == recipeId);
    }

    public async Task SaveChangesAsync()
    {
        await context.SaveChangesAsync();
    }
}

