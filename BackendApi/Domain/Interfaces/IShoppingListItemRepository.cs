using BackendApi.Domain.Entities;

namespace BackendApi.Domain.Interfaces;

public interface IShoppingListItemRepository
{
    Task<List<ShoppingListItem>> GetByShoppingListIdAsync(int shoppingListId);
    Task<ShoppingListItem?> GetByIdAsync(int id);
    Task<ShoppingListItem> AddAsync(ShoppingListItem item);
    Task UpdateAsync(ShoppingListItem item);
    Task DeleteAsync(ShoppingListItem item);
    Task DeleteByShoppingListIdAsync(int shoppingListId);
    Task SaveChangesAsync();
}

