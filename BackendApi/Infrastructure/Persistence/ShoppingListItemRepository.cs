using BackendApi.Domain.Entities;
using BackendApi.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace BackendApi.Infrastructure.Persistence;

public class ShoppingListItemRepository(AppDbContext context) : IShoppingListItemRepository
{
    private readonly AppDbContext _context = context;

    public async Task<List<ShoppingListItem>> GetByShoppingListIdAsync(int shoppingListId)
        => await _context.ShoppingListItems
            .Where(sli => sli.ShoppingListId == shoppingListId)
            .OrderBy(sli => sli.DisplayOrder)
            .ThenBy(sli => sli.Ingredient)
            .ToListAsync();

    public async Task<ShoppingListItem?> GetByIdAsync(int id)
        => await _context.ShoppingListItems.FindAsync(id);

    public async Task<ShoppingListItem> AddAsync(ShoppingListItem item)
    {
        await _context.ShoppingListItems.AddAsync(item);
        return item;
    }

    public Task UpdateAsync(ShoppingListItem item)
    {
        _context.ShoppingListItems.Update(item);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(ShoppingListItem item)
    {
        _context.ShoppingListItems.Remove(item);
        return Task.CompletedTask;
    }

    public async Task DeleteByShoppingListIdAsync(int shoppingListId)
    {
        var items = await _context.ShoppingListItems
            .Where(sli => sli.ShoppingListId == shoppingListId)
            .ToListAsync();
        _context.ShoppingListItems.RemoveRange(items);
    }

    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }
}

