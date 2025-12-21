using BackendApi.Domain.Entities;
using BackendApi.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace BackendApi.Infrastructure.Persistence;

public class CommentRepository(AppDbContext context) : ICommentRepository
{
    public async Task<RecipeComment?> GetByIdAsync(int id)
    {
        return await context.RecipeComments
            .Include(c => c.CommentLikes)
            .Include(c => c.ParentComment)
            .Include(c => c.Replies.Where(r => !r.IsDeleted))
            .FirstOrDefaultAsync(c => c.Id == id && !c.IsDeleted);
    }

    public async Task<List<RecipeComment>> GetByRecipeIdAsync(int recipeId, int? skip = null, int? take = null)
    {
        IQueryable<RecipeComment> query = context.RecipeComments
            .Include(c => c.CommentLikes)
            .Include(c => c.ParentComment)
            .Include(c => c.Replies.Where(r => !r.IsDeleted))
            .Where(c => c.RecipeId == recipeId && !c.IsDeleted)
            .OrderByDescending(c => c.CreatedAt);

        if (skip.HasValue)
            query = query.Skip(skip.Value);

        if (take.HasValue)
            query = query.Take(take.Value);

        return await query.ToListAsync();
    }

    public async Task<List<RecipeComment>> GetByUserIdAsync(string userId)
    {
        return await context.RecipeComments
            .Include(c => c.Recipe)
            .Where(c => c.UserId == userId && !c.IsDeleted)
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync();
    }

    public async Task<int> GetCountByRecipeIdAsync(int recipeId)
    {
        return await context.RecipeComments
            .CountAsync(c => c.RecipeId == recipeId && !c.IsDeleted);
    }

    public async Task<RecipeComment> AddAsync(RecipeComment comment)
    {
        await context.RecipeComments.AddAsync(comment);
        return comment;
    }

    public async Task UpdateAsync(RecipeComment comment)
    {
        comment.UpdatedAt = DateTime.UtcNow;
        context.RecipeComments.Update(comment);
        await Task.CompletedTask;
    }

    public async Task DeleteAsync(RecipeComment comment)
    {
        // Soft delete
        comment.IsDeleted = true;
        comment.DeletedAt = DateTime.UtcNow;
        context.RecipeComments.Update(comment);
        await Task.CompletedTask;
    }

    public async Task SaveChangesAsync()
    {
        await context.SaveChangesAsync();
    }
}

