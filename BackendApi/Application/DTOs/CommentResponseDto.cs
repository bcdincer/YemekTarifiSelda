namespace BackendApi.Application.DTOs;

public class CommentResponseDto
{
    public int Id { get; set; }
    public int RecipeId { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public int LikeCount { get; set; }
    public bool IsLikedByUser { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public bool CanEdit { get; set; } // Kullanıcı bu yorumu düzenleyebilir mi?
    public bool CanDelete { get; set; } // Kullanıcı bu yorumu silebilir mi?
    
    // Yorum yanıtlama için
    public int? ParentCommentId { get; set; } // Ana yorum ID (null ise ana yorum, değilse yanıt)
    public List<CommentResponseDto> Replies { get; set; } = new(); // Yanıtlar
    public int ReplyCount { get; set; } // Toplam yanıt sayısı
}

