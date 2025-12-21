using BackendApi.Domain.Entities;
using BackendApi.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace BackendApi.Infrastructure.Persistence;

public class MealPlanRepository(AppDbContext context) : IMealPlanRepository
{
    private readonly AppDbContext _context = context;

    public async Task<MealPlan?> GetByIdAsync(int id)
        => await _context.MealPlans
            .Include(mp => mp.Items)
                .ThenInclude(mpi => mpi.Recipe)
            .FirstOrDefaultAsync(mp => mp.Id == id);

    public async Task<List<MealPlan>> GetByUserIdAsync(string userId)
        => await _context.MealPlans
            .Include(mp => mp.Items)
                .ThenInclude(mpi => mpi.Recipe)
            .Where(mp => mp.UserId == userId)
            .OrderByDescending(mp => mp.StartDate)
            .ToListAsync();

    public async Task<MealPlan?> GetByUserIdAndDateRangeAsync(string userId, DateTime startDate, DateTime endDate)
        => await _context.MealPlans
            .Include(mp => mp.Items)
                .ThenInclude(mpi => mpi.Recipe)
            .Where(mp => mp.UserId == userId &&
                        mp.StartDate <= endDate &&
                        mp.EndDate >= startDate)
            .FirstOrDefaultAsync();

    public async Task<MealPlan> AddAsync(MealPlan mealPlan)
    {
        await _context.MealPlans.AddAsync(mealPlan);
        return mealPlan;
    }

    public Task UpdateAsync(MealPlan mealPlan)
    {
        _context.MealPlans.Update(mealPlan);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(MealPlan mealPlan)
    {
        _context.MealPlans.Remove(mealPlan);
        return Task.CompletedTask;
    }

    public async Task SaveChangesAsync()
    {
        await _context.SaveChangesAsync();
    }
}

