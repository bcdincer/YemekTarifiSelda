using BackendApi.Domain.Entities;
using BackendApi.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace BackendApi.Infrastructure.Persistence;

public class CollectionRepository(AppDbContext context) : ICollectionRepository
{
    public async Task<Collection?> GetByIdAsync(int id)
    {
        return await context.Collections
            .Include(c => c.CollectionRecipes)
                .ThenInclude(cr => cr.Recipe)
            .FirstOrDefaultAsync(c => c.Id == id);
    }

    public async Task<List<Collection>> GetByUserIdAsync(string userId)
    {
        return await context.Collections
            .Include(c => c.CollectionRecipes)
            .Where(c => c.UserId == userId)
            .OrderByDescending(c => c.UpdatedAt)
            .ToListAsync();
    }

    public async Task<Collection> AddAsync(Collection collection)
    {
        await context.Collections.AddAsync(collection);
        return collection;
    }

    public async Task UpdateAsync(Collection collection)
    {
        collection.UpdatedAt = DateTime.UtcNow;
        context.Collections.Update(collection);
        await Task.CompletedTask;
    }

    public async Task DeleteAsync(Collection collection)
    {
        context.Collections.Remove(collection);
        await Task.CompletedTask;
    }

    public async Task<bool> ExistsAsync(int id, string userId)
    {
        return await context.Collections
            .AnyAsync(c => c.Id == id && c.UserId == userId);
    }

    public async Task<bool> NameExistsForUserAsync(string name, string userId, int? excludeId = null)
    {
        var query = context.Collections
            .Where(c => c.UserId == userId && c.Name.ToLower() == name.ToLower());

        if (excludeId.HasValue)
        {
            query = query.Where(c => c.Id != excludeId.Value);
        }

        return await query.AnyAsync();
    }

    public async Task SaveChangesAsync()
    {
        await context.SaveChangesAsync();
    }
}

