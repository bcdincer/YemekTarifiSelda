using BackendApi.Application.DTOs;

namespace BackendApi.Application.Services;

public interface IShoppingListService
{
    Task<ShoppingListResponseDto?> GetByIdAsync(int id, string userId);
    Task<List<ShoppingListResponseDto>> GetByUserIdAsync(string userId);
    Task<ShoppingListResponseDto> CreateAsync(CreateShoppingListDto dto, string userId);
    Task<ShoppingListResponseDto> CreateFromMealPlanAsync(int mealPlanId, string userId);
    Task<bool> UpdateAsync(int id, CreateShoppingListDto dto, string userId);
    Task<bool> UpdateItemCheckedAsync(int listId, int itemId, bool isChecked, string userId);
    Task<bool> DeleteAsync(int id, string userId);
}

