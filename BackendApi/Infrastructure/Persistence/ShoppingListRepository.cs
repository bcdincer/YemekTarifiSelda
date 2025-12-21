using BackendApi.Domain.Entities;
using BackendApi.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace BackendApi.Infrastructure.Persistence;

public class ShoppingListRepository(AppDbContext context) : IShoppingListRepository
{
    private readonly AppDbContext _context = context;

    public async Task<ShoppingList?> GetByIdAsync(int id)
        => await _context.ShoppingLists
            .Include(sl => sl.Items)
            .FirstOrDefaultAsync(sl => sl.Id == id);

    public async Task<List<ShoppingList>> GetByUserIdAsync(string userId)
        => await _context.ShoppingLists
            .Include(sl => sl.Items)
            .Where(sl => sl.UserId == userId)
            .OrderByDescending(sl => sl.CreatedAt)
            .ToListAsync();

    public async Task<ShoppingList?> GetByMealPlanIdAsync(int mealPlanId)
        => await _context.ShoppingLists
            .Include(sl => sl.Items)
            .FirstOrDefaultAsync(sl => sl.MealPlanId == mealPlanId);

    public async Task<ShoppingList> AddAsync(ShoppingList shoppingList)
    {
        await _context.ShoppingLists.AddAsync(shoppingList);
        return shoppingList;
    }

    public Task UpdateAsync(ShoppingList shoppingList)
    {
        _context.ShoppingLists.Update(shoppingList);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(ShoppingList shoppingList)
    {
        _context.ShoppingLists.Remove(shoppingList);
        return Task.CompletedTask;
    }

    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }
}

