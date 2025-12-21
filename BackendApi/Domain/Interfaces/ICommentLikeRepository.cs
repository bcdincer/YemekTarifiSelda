using BackendApi.Domain.Entities;

namespace BackendApi.Domain.Interfaces;

public interface ICommentLikeRepository
{
    Task<CommentLike?> GetByCommentAndUserAsync(int commentId, string userId);
    Task<int> GetLikeCountAsync(int commentId);
    Task<Dictionary<int, int>> GetLikeCountsAsync(List<int> commentIds);
    Task<Dictionary<int, bool>> GetUserLikesAsync(List<int> commentIds, string userId);
    Task<CommentLike> AddAsync(CommentLike like);
    Task DeleteAsync(CommentLike like);
    Task<bool> ExistsAsync(int commentId, string userId);
    Task SaveChangesAsync();
}

