using BackendApi.Domain.Entities;
using BackendApi.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace BackendApi.Infrastructure.Persistence;

public class CommentLikeRepository(AppDbContext context) : ICommentLikeRepository
{
    public async Task<CommentLike?> GetByCommentAndUserAsync(int commentId, string userId)
    {
        return await context.CommentLikes
            .FirstOrDefaultAsync(cl => cl.CommentId == commentId && cl.UserId == userId);
    }

    public async Task<int> GetLikeCountAsync(int commentId)
    {
        return await context.CommentLikes
            .CountAsync(cl => cl.CommentId == commentId);
    }

    public async Task<Dictionary<int, int>> GetLikeCountsAsync(List<int> commentIds)
    {
        if (!commentIds.Any())
            return new Dictionary<int, int>();

        var counts = await context.CommentLikes
            .Where(cl => commentIds.Contains(cl.CommentId))
            .GroupBy(cl => cl.CommentId)
            .Select(g => new
            {
                CommentId = g.Key,
                Count = g.Count()
            })
            .ToListAsync();

        var result = new Dictionary<int, int>();
        foreach (var commentId in commentIds)
        {
            var count = counts.FirstOrDefault(c => c.CommentId == commentId);
            result[commentId] = count?.Count ?? 0;
        }

        return result;
    }

    public async Task<Dictionary<int, bool>> GetUserLikesAsync(List<int> commentIds, string userId)
    {
        if (!commentIds.Any())
            return new Dictionary<int, bool>();

        var userLikes = await context.CommentLikes
            .Where(cl => commentIds.Contains(cl.CommentId) && cl.UserId == userId)
            .Select(cl => cl.CommentId)
            .ToListAsync();

        var result = new Dictionary<int, bool>();
        foreach (var commentId in commentIds)
        {
            result[commentId] = userLikes.Contains(commentId);
        }

        return result;
    }

    public async Task<CommentLike> AddAsync(CommentLike like)
    {
        await context.CommentLikes.AddAsync(like);
        return like;
    }

    public async Task DeleteAsync(CommentLike like)
    {
        context.CommentLikes.Remove(like);
        await Task.CompletedTask;
    }

    public async Task<bool> ExistsAsync(int commentId, string userId)
    {
        return await context.CommentLikes
            .AnyAsync(cl => cl.CommentId == commentId && cl.UserId == userId);
    }

    public async Task SaveChangesAsync()
    {
        await context.SaveChangesAsync();
    }
}

