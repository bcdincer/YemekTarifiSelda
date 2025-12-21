using BackendApi.Domain.Entities;
using BackendApi.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace BackendApi.Infrastructure.Persistence;

public class RecipeRepository(AppDbContext context) : IRecipeRepository
{
    private readonly AppDbContext _context = context;

    public async Task<List<Recipe>> GetAllAsync()
        => await _context.Recipes
            .Include(r => r.Category)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();

    public async Task<(List<Recipe> Items, int TotalCount)> GetAllPagedAsync(int pageNumber, int pageSize)
    {
        var query = _context.Recipes.Include(r => r.Category).OrderByDescending(r => r.CreatedAt);
        
        var totalCount = await query.CountAsync();
        
        var items = await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
        
        return (items, totalCount);
    }

    public async Task<Recipe?> GetByIdAsync(int id)
        => await _context.Recipes
            .Include(r => r.Category)
            .FirstOrDefaultAsync(r => r.Id == id);

    public async Task AddAsync(Recipe recipe)
    {
        await _context.Recipes.AddAsync(recipe);
    }

    public Task UpdateAsync(Recipe recipe)
    {
        _context.Recipes.Update(recipe);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(Recipe recipe)
    {
        _context.Recipes.Remove(recipe);
        return Task.CompletedTask;
    }

    public async Task<List<Recipe>> GetFeaturedAsync(int count = 6)
        => await _context.Recipes
            .Include(r => r.Category)
            .Where(r => r.IsFeatured)
            .OrderByDescending(r => r.CreatedAt)
            .Take(count)
            .ToListAsync();

    public async Task<List<Recipe>> GetPopularAsync(int count = 6)
        => await _context.Recipes
            .Include(r => r.Category)
            .OrderByDescending(r => r.ViewCount)
            .ThenByDescending(r => r.CreatedAt)
            .Take(count)
            .ToListAsync();

    public async Task<List<Recipe>> GetByCategoryAsync(int categoryId)
        => await _context.Recipes
            .Include(r => r.Category)
            .Where(r => r.CategoryId == categoryId)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();

    public async Task<(List<Recipe> Items, int TotalCount)> GetByCategoryPagedAsync(int categoryId, int pageNumber, int pageSize)
    {
        var query = _context.Recipes
            .Include(r => r.Category)
            .Where(r => r.CategoryId == categoryId)
            .OrderByDescending(r => r.CreatedAt);
        
        var totalCount = await query.CountAsync();
        
        var items = await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
        
        return (items, totalCount);
    }

    public async Task<List<Recipe>> SearchAsync(string searchTerm)
    {
        var term = searchTerm.ToLower();
        return await _context.Recipes
            .Include(r => r.Category)
            .Where(r => 
                r.Title.ToLower().Contains(term) ||
                r.Description.ToLower().Contains(term) ||
                r.Ingredients.ToLower().Contains(term) ||
                r.Steps.ToLower().Contains(term) ||
                (r.Tips != null && r.Tips.ToLower().Contains(term)) ||
                (r.AlternativeIngredients != null && r.AlternativeIngredients.ToLower().Contains(term)) ||
                (r.Category != null && r.Category.Name.ToLower().Contains(term)))
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync();
    }

    public async Task<(List<Recipe> Items, int TotalCount)> SearchPagedAsync(string searchTerm, int pageNumber, int pageSize)
    {
        var term = searchTerm.ToLower();
        var query = _context.Recipes
            .Include(r => r.Category)
            .Where(r => 
                r.Title.ToLower().Contains(term) ||
                r.Description.ToLower().Contains(term) ||
                r.Ingredients.ToLower().Contains(term) ||
                r.Steps.ToLower().Contains(term) ||
                (r.Tips != null && r.Tips.ToLower().Contains(term)) ||
                (r.AlternativeIngredients != null && r.AlternativeIngredients.ToLower().Contains(term)) ||
                (r.Category != null && r.Category.Name.ToLower().Contains(term)))
            .OrderByDescending(r => r.CreatedAt);
        
        var totalCount = await query.CountAsync();
        
        var items = await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
        
        return (items, totalCount);
    }

    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }
}


