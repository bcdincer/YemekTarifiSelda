using BackendApi.Domain.Entities;

namespace BackendApi.Domain.Interfaces;

public interface IMealPlanItemRepository
{
    Task<List<MealPlanItem>> GetByMealPlanIdAsync(int mealPlanId);
    Task<List<MealPlanItem>> GetByMealPlanIdAndDateAsync(int mealPlanId, DateTime date);
    Task<MealPlanItem?> GetByIdAsync(int id);
    Task<MealPlanItem> AddAsync(MealPlanItem item);
    Task UpdateAsync(MealPlanItem item);
    Task DeleteAsync(MealPlanItem item);
    Task DeleteByMealPlanIdAsync(int mealPlanId);
    Task SaveChangesAsync();
}

