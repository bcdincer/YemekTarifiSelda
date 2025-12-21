using BackendApi.Application.DTOs;

namespace BackendApi.Application.Services;

public interface IMealPlanService
{
    Task<MealPlanResponseDto?> GetByIdAsync(int id, string userId);
    Task<List<MealPlanResponseDto>> GetByUserIdAsync(string userId);
    Task<MealPlanResponseDto?> GetByUserIdAndDateRangeAsync(string userId, DateTime startDate, DateTime endDate);
    Task<MealPlanResponseDto> CreateAsync(CreateMealPlanDto dto, string userId);
    Task<bool> UpdateAsync(int id, CreateMealPlanDto dto, string userId);
    Task<bool> DeleteAsync(int id, string userId);
}

