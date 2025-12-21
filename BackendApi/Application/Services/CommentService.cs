using BackendApi.Application.DTOs;
using BackendApi.Domain.Entities;
using BackendApi.Domain.Interfaces;

namespace BackendApi.Application.Services;

public class CommentService(
    IUnitOfWork unitOfWork,
    ILogger<CommentService> logger) : ICommentService
{
    private ICommentRepository CommentRepository => unitOfWork.Comments;
    private ICommentLikeRepository CommentLikeRepository => unitOfWork.CommentLikes;
    private IRecipeRepository RecipeRepository => unitOfWork.Recipes;

    public async Task<CommentResponseDto> CreateAsync(int recipeId, CreateCommentDto dto, string userId, string userName)
    {
        // Tarif var mı kontrol et
        var recipe = await RecipeRepository.GetByIdAsync(recipeId);
        if (recipe == null)
            throw new ArgumentException($"Recipe with id {recipeId} not found", nameof(recipeId));

        // Eğer yanıt veriliyorsa, parent comment var mı kontrol et
        if (dto.ParentCommentId.HasValue)
        {
            var parentComment = await CommentRepository.GetByIdAsync(dto.ParentCommentId.Value);
            if (parentComment == null || parentComment.RecipeId != recipeId)
                throw new ArgumentException($"Parent comment with id {dto.ParentCommentId.Value} not found or does not belong to this recipe", nameof(dto.ParentCommentId));
        }

        var comment = new RecipeComment
        {
            RecipeId = recipeId,
            UserId = userId,
            UserName = userName,
            Content = dto.Content.Trim(),
            ParentCommentId = dto.ParentCommentId,
            CreatedAt = DateTime.UtcNow
        };

        await CommentRepository.AddAsync(comment);
        await unitOfWork.SaveChangesAsync();

        logger.LogInformation("Comment created for recipe {RecipeId} by user {UserId} (ParentCommentId: {ParentCommentId})", 
            recipeId, userId, dto.ParentCommentId);

        return MapToDto(comment, userId);
    }

    public async Task<CommentResponseDto?> GetByIdAsync(int id, string? currentUserId = null)
    {
        var comment = await CommentRepository.GetByIdAsync(id);
        if (comment == null)
            return null;

        return MapToDto(comment, currentUserId);
    }

    public async Task<List<CommentResponseDto>> GetByRecipeIdAsync(int recipeId, int? skip = null, int? take = null, string? currentUserId = null)
    {
        // Sadece ana yorumları getir (ParentCommentId null olanlar)
        var allComments = await CommentRepository.GetByRecipeIdAsync(recipeId, null, null); // Tüm yorumları getir
        
        if (!allComments.Any())
            return new List<CommentResponseDto>();

        // Ana yorumları filtrele (ParentCommentId null olanlar)
        var parentComments = allComments.Where(c => c.ParentCommentId == null).ToList();
        
        // Pagination uygula (sadece ana yorumlara)
        var paginatedParentComments = parentComments
            .Skip(skip ?? 0)
            .Take(take ?? 50)
            .ToList();

        // Tüm yorum ID'lerini topla (ana yorumlar + yanıtlar)
        var allCommentIds = allComments.Select(c => c.Id).ToList();
        
        // Bulk fetch like counts and user likes
        var likeCounts = await CommentLikeRepository.GetLikeCountsAsync(allCommentIds);
        var userLikes = currentUserId != null 
            ? await CommentLikeRepository.GetUserLikesAsync(allCommentIds, currentUserId)
            : new Dictionary<int, bool>();

        // Hiyerarşik yapı oluştur
        var result = new List<CommentResponseDto>();
        foreach (var parentComment in paginatedParentComments)
        {
            var parentDto = MapToDtoWithReplies(parentComment, allComments, likeCounts, userLikes, currentUserId);
            result.Add(parentDto);
        }

        return result;
    }

    private CommentResponseDto MapToDtoWithReplies(
        RecipeComment comment, 
        List<RecipeComment> allComments, 
        Dictionary<int, int> likeCounts, 
        Dictionary<int, bool> userLikes, 
        string? currentUserId)
    {
        var dto = MapToDto(comment, currentUserId);
        dto.LikeCount = likeCounts.GetValueOrDefault(comment.Id, 0);
        dto.IsLikedByUser = userLikes.GetValueOrDefault(comment.Id, false);
        
        // Yanıtları bul ve ekle
        var replies = allComments
            .Where(c => c.ParentCommentId == comment.Id)
            .OrderBy(c => c.CreatedAt)
            .ToList();
        
        dto.ReplyCount = replies.Count;
        dto.Replies = replies.Select(reply => 
        {
            var replyDto = MapToDto(reply, currentUserId);
            replyDto.LikeCount = likeCounts.GetValueOrDefault(reply.Id, 0);
            replyDto.IsLikedByUser = userLikes.GetValueOrDefault(reply.Id, false);
            return replyDto;
        }).ToList();

        return dto;
    }

    public async Task<int> GetCountByRecipeIdAsync(int recipeId)
    {
        return await CommentRepository.GetCountByRecipeIdAsync(recipeId);
    }

    public async Task<CommentResponseDto> UpdateAsync(int id, UpdateCommentDto dto, string userId)
    {
        var comment = await CommentRepository.GetByIdAsync(id);
        if (comment == null)
            throw new ArgumentException($"Comment with id {id} not found", nameof(id));

        if (comment.UserId != userId)
            throw new UnauthorizedAccessException("You can only edit your own comments");

        comment.Content = dto.Content.Trim();
        await CommentRepository.UpdateAsync(comment);
        await unitOfWork.SaveChangesAsync();

        logger.LogInformation("Comment {CommentId} updated by user {UserId}", id, userId);

        return MapToDto(comment, userId);
    }

    public async Task DeleteAsync(int id, string userId)
    {
        var comment = await CommentRepository.GetByIdAsync(id);
        if (comment == null)
            throw new ArgumentException($"Comment with id {id} not found", nameof(id));

        if (comment.UserId != userId)
            throw new UnauthorizedAccessException("You can only delete your own comments");

        await CommentRepository.DeleteAsync(comment);
        await unitOfWork.SaveChangesAsync();

        logger.LogInformation("Comment {CommentId} deleted by user {UserId}", id, userId);
    }

    public async Task ToggleLikeAsync(int commentId, string userId)
    {
        try
        {
            await unitOfWork.BeginTransactionAsync();

            var existingLike = await CommentLikeRepository.GetByCommentAndUserAsync(commentId, userId);

            if (existingLike != null)
            {
                // Beğeniyi kaldır
                await CommentLikeRepository.DeleteAsync(existingLike);
            }
            else
            {
                // Beğeni ekle
                var newLike = new CommentLike
                {
                    CommentId = commentId,
                    UserId = userId,
                    CreatedAt = DateTime.UtcNow
                };
                await CommentLikeRepository.AddAsync(newLike);
            }

            // Comment'in like count'unu güncelle
            var likeCount = await CommentLikeRepository.GetLikeCountAsync(commentId);
            var comment = await CommentRepository.GetByIdAsync(commentId);
            if (comment != null)
            {
                comment.LikeCount = likeCount;
                await CommentRepository.UpdateAsync(comment);
            }

            await unitOfWork.CommitTransactionAsync();
            logger.LogInformation("Like toggled for comment {CommentId} by user {UserId}", commentId, userId);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error toggling like for comment {CommentId} by user {UserId}", commentId, userId);
            await unitOfWork.RollbackTransactionAsync();
            throw;
        }
    }

    public async Task<bool> IsLikedByUserAsync(int commentId, string userId)
    {
        return await CommentLikeRepository.ExistsAsync(commentId, userId);
    }

    private CommentResponseDto MapToDto(RecipeComment comment, string? currentUserId)
    {
        return new CommentResponseDto
        {
            Id = comment.Id,
            RecipeId = comment.RecipeId,
            UserId = comment.UserId,
            UserName = comment.UserName,
            Content = comment.Content,
            LikeCount = comment.LikeCount,
            IsLikedByUser = false, // Will be set by caller if needed
            CreatedAt = comment.CreatedAt,
            UpdatedAt = comment.UpdatedAt,
            CanEdit = currentUserId != null && comment.UserId == currentUserId,
            CanDelete = currentUserId != null && comment.UserId == currentUserId,
            ParentCommentId = comment.ParentCommentId,
            Replies = new List<CommentResponseDto>(),
            ReplyCount = 0
        };
    }
}

