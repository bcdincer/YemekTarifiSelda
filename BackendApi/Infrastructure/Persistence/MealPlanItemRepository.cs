using BackendApi.Domain.Entities;
using BackendApi.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace BackendApi.Infrastructure.Persistence;

public class MealPlanItemRepository(AppDbContext context) : IMealPlanItemRepository
{
    private readonly AppDbContext _context = context;

    public async Task<List<MealPlanItem>> GetByMealPlanIdAsync(int mealPlanId)
        => await _context.MealPlanItems
            .Include(mpi => mpi.Recipe)
            .Where(mpi => mpi.MealPlanId == mealPlanId)
            .OrderBy(mpi => mpi.Date)
            .ThenBy(mpi => mpi.MealType)
            .ThenBy(mpi => mpi.DisplayOrder)
            .ToListAsync();

    public async Task<List<MealPlanItem>> GetByMealPlanIdAndDateAsync(int mealPlanId, DateTime date)
        => await _context.MealPlanItems
            .Include(mpi => mpi.Recipe)
            .Where(mpi => mpi.MealPlanId == mealPlanId && mpi.Date.Date == date.Date)
            .OrderBy(mpi => mpi.MealType)
            .ThenBy(mpi => mpi.DisplayOrder)
            .ToListAsync();

    public async Task<MealPlanItem?> GetByIdAsync(int id)
        => await _context.MealPlanItems
            .Include(mpi => mpi.Recipe)
            .FirstOrDefaultAsync(mpi => mpi.Id == id);

    public async Task<MealPlanItem> AddAsync(MealPlanItem item)
    {
        await _context.MealPlanItems.AddAsync(item);
        return item;
    }

    public Task UpdateAsync(MealPlanItem item)
    {
        _context.MealPlanItems.Update(item);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(MealPlanItem item)
    {
        _context.MealPlanItems.Remove(item);
        return Task.CompletedTask;
    }

    public async Task DeleteByMealPlanIdAsync(int mealPlanId)
    {
        var items = await _context.MealPlanItems
            .Where(mpi => mpi.MealPlanId == mealPlanId)
            .ToListAsync();
        _context.MealPlanItems.RemoveRange(items);
    }

    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }
}

