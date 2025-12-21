using BackendApi.Domain.Entities;

namespace BackendApi.Domain.Interfaces;

public interface IMealPlanRepository
{
    Task<MealPlan?> GetByIdAsync(int id);
    Task<List<MealPlan>> GetByUserIdAsync(string userId);
    Task<MealPlan?> GetByUserIdAndDateRangeAsync(string userId, DateTime startDate, DateTime endDate);
    Task<MealPlan> AddAsync(MealPlan mealPlan);
    Task UpdateAsync(MealPlan mealPlan);
    Task DeleteAsync(MealPlan mealPlan);
    Task SaveChangesAsync();
}

