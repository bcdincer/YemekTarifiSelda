using BackendApi.Application.DTOs;

namespace BackendApi.Application.Services;

public interface ICommentService
{
    Task<CommentResponseDto> CreateAsync(int recipeId, CreateCommentDto dto, string userId, string userName);
    Task<CommentResponseDto?> GetByIdAsync(int id, string? currentUserId = null);
    Task<List<CommentResponseDto>> GetByRecipeIdAsync(int recipeId, int? skip = null, int? take = null, string? currentUserId = null);
    Task<int> GetCountByRecipeIdAsync(int recipeId);
    Task<CommentResponseDto> UpdateAsync(int id, UpdateCommentDto dto, string userId);
    Task DeleteAsync(int id, string userId);
    Task ToggleLikeAsync(int commentId, string userId);
    Task<bool> IsLikedByUserAsync(int commentId, string userId);
}

