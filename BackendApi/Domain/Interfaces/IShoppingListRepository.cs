using BackendApi.Domain.Entities;

namespace BackendApi.Domain.Interfaces;

public interface IShoppingListRepository
{
    Task<ShoppingList?> GetByIdAsync(int id);
    Task<List<ShoppingList>> GetByUserIdAsync(string userId);
    Task<ShoppingList?> GetByMealPlanIdAsync(int mealPlanId);
    Task<ShoppingList> AddAsync(ShoppingList shoppingList);
    Task UpdateAsync(ShoppingList shoppingList);
    Task DeleteAsync(ShoppingList shoppingList);
    Task SaveChangesAsync();
}

