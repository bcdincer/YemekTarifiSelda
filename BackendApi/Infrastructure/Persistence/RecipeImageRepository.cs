using BackendApi.Domain.Entities;
using BackendApi.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace BackendApi.Infrastructure.Persistence;

public class RecipeImageRepository(AppDbContext context) : IRecipeImageRepository
{
    public async Task<List<RecipeImage>> GetByRecipeIdAsync(int recipeId)
    {
        return await context.RecipeImages
            .Where(ri => ri.RecipeId == recipeId)
            .OrderBy(ri => ri.DisplayOrder)
            .ToListAsync();
    }

    public async Task<RecipeImage?> GetByIdAsync(int id)
    {
        return await context.RecipeImages
            .FirstOrDefaultAsync(ri => ri.Id == id);
    }

    public async Task<RecipeImage> AddAsync(RecipeImage image)
    {
        await context.RecipeImages.AddAsync(image);
        return image;
    }

    public async Task DeleteAsync(RecipeImage image)
    {
        context.RecipeImages.Remove(image);
        await Task.CompletedTask;
    }

    public async Task DeleteRangeAsync(IEnumerable<RecipeImage> images)
    {
        context.RecipeImages.RemoveRange(images);
        await Task.CompletedTask;
    }

    public async Task UpdateAsync(RecipeImage image)
    {
        context.RecipeImages.Update(image);
        await Task.CompletedTask;
    }
}

