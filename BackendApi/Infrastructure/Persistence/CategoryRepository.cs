using BackendApi.Domain.Entities;
using BackendApi.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace BackendApi.Infrastructure.Persistence;

public class CategoryRepository(AppDbContext context) : ICategoryRepository
{
    private readonly AppDbContext _context = context;

    public async Task<List<Category>> GetAllAsync()
        => await _context.Categories.OrderBy(c => c.DisplayOrder).ThenBy(c => c.Name).ToListAsync();

    public async Task<Category?> GetByIdAsync(int id)
        => await _context.Categories.FindAsync(id);

    public async Task AddAsync(Category category)
    {
        await _context.Categories.AddAsync(category);
    }

    public Task UpdateAsync(Category category)
    {
        _context.Categories.Update(category);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(Category category)
    {
        _context.Categories.Remove(category);
        return Task.CompletedTask;
    }

    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }
}

