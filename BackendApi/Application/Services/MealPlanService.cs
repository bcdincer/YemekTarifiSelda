using BackendApi.Application.DTOs;
using BackendApi.Application.Mappings;
using BackendApi.Domain.Entities;
using BackendApi.Domain.Interfaces;

namespace BackendApi.Application.Services;

public class MealPlanService : IMealPlanService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<MealPlanService> _logger;
    private IMealPlanRepository MealPlanRepository => _unitOfWork.MealPlans;
    private IMealPlanItemRepository MealPlanItemRepository => _unitOfWork.MealPlanItems;

    public MealPlanService(IUnitOfWork unitOfWork, ILogger<MealPlanService> logger)
    {
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<MealPlanResponseDto?> GetByIdAsync(int id, string userId)
    {
        var mealPlan = await MealPlanRepository.GetByIdAsync(id);
        if (mealPlan == null || mealPlan.UserId != userId)
            return null;

        return mealPlan.ToDto();
    }

    public async Task<List<MealPlanResponseDto>> GetByUserIdAsync(string userId)
    {
        var mealPlans = await MealPlanRepository.GetByUserIdAsync(userId);
        return mealPlans.Select(mp => mp.ToDto()).ToList();
    }

    public async Task<MealPlanResponseDto?> GetByUserIdAndDateRangeAsync(string userId, DateTime startDate, DateTime endDate)
    {
        var mealPlan = await MealPlanRepository.GetByUserIdAndDateRangeAsync(userId, startDate, endDate);
        if (mealPlan == null)
            return null;

        return mealPlan.ToDto();
    }

    public async Task<MealPlanResponseDto> CreateAsync(CreateMealPlanDto dto, string userId)
    {
        var mealPlan = new MealPlan
        {
            UserId = userId,
            Name = dto.Name,
            StartDate = DateTime.SpecifyKind(dto.StartDate.Date, DateTimeKind.Utc),
            EndDate = DateTime.SpecifyKind(dto.EndDate.Date, DateTimeKind.Utc),
            CreatedAt = DateTime.UtcNow
        };

        await MealPlanRepository.AddAsync(mealPlan);
        await _unitOfWork.SaveChangesAsync();

        // Items ekle
        var displayOrder = 0;
        foreach (var itemDto in dto.Items)
        {
            // RecipeId kontrolü
            var recipeExists = await _unitOfWork.Recipes.GetByIdAsync(itemDto.RecipeId);
            if (recipeExists == null)
            {
                throw new ArgumentException($"Recipe with id {itemDto.RecipeId} not found.");
            }

            if (!Enum.TryParse<MealType>(itemDto.MealType, true, out var mealType))
            {
                _logger.LogWarning("Invalid MealType '{MealType}', defaulting to AkşamYemeği", itemDto.MealType);
                mealType = MealType.AkşamYemeği;
            }

            var item = new MealPlanItem
            {
                MealPlanId = mealPlan.Id,
                RecipeId = itemDto.RecipeId,
                Date = DateTime.SpecifyKind(itemDto.Date.Date, DateTimeKind.Utc),
                MealType = mealType,
                Servings = itemDto.Servings,
                DisplayOrder = displayOrder++,
                CreatedAt = DateTime.UtcNow
            };

            await MealPlanItemRepository.AddAsync(item);
        }

        try
        {
            await _unitOfWork.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error saving meal plan items");
            throw;
        }

        var created = await MealPlanRepository.GetByIdAsync(mealPlan.Id);
        return created!.ToDto();
    }

    public async Task<bool> UpdateAsync(int id, CreateMealPlanDto dto, string userId)
    {
        var existing = await MealPlanRepository.GetByIdAsync(id);
        if (existing == null || existing.UserId != userId)
            return false;

        existing.Name = dto.Name;
        existing.StartDate = DateTime.SpecifyKind(dto.StartDate.Date, DateTimeKind.Utc);
        existing.EndDate = DateTime.SpecifyKind(dto.EndDate.Date, DateTimeKind.Utc);
        existing.UpdatedAt = DateTime.UtcNow;

        // Mevcut items'ı sil
        await MealPlanItemRepository.DeleteByMealPlanIdAsync(id);
        await _unitOfWork.SaveChangesAsync();

        // Yeni items ekle
        var displayOrder = 0;
        foreach (var itemDto in dto.Items)
        {
            if (!Enum.TryParse<MealType>(itemDto.MealType, true, out var mealType))
                mealType = MealType.AkşamYemeği;

            var item = new MealPlanItem
            {
                MealPlanId = id,
                RecipeId = itemDto.RecipeId,
                Date = DateTime.SpecifyKind(itemDto.Date.Date, DateTimeKind.Utc),
                MealType = mealType,
                Servings = itemDto.Servings,
                DisplayOrder = displayOrder++,
                CreatedAt = DateTime.UtcNow
            };

            await MealPlanItemRepository.AddAsync(item);
        }

        await MealPlanRepository.UpdateAsync(existing);
        await _unitOfWork.SaveChangesAsync();

        return true;
    }

    public async Task<bool> DeleteAsync(int id, string userId)
    {
        var mealPlan = await MealPlanRepository.GetByIdAsync(id);
        if (mealPlan == null || mealPlan.UserId != userId)
            return false;

        await MealPlanRepository.DeleteAsync(mealPlan);
        await _unitOfWork.SaveChangesAsync();

        return true;
    }
}

