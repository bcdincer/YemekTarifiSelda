namespace BackendApi.Domain.Entities;

public class RecipeComment
{
    public int Id { get; set; }
    public int RecipeId { get; set; }
    public Recipe Recipe { get; set; } = null!;
    public string UserId { get; set; } = string.Empty; // Kullanıcı ID
    public string UserName { get; set; } = string.Empty; // Kullanıcı adı (cache için)
    public string Content { get; set; } = string.Empty; // Yorum içeriği
    public int LikeCount { get; set; } = 0; // Beğeni sayısı
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public bool IsDeleted { get; set; } = false; // Soft delete
    public DateTime? DeletedAt { get; set; }
    
    // Yorum yanıtlama için
    public int? ParentCommentId { get; set; } // Ana yorum ID (null ise ana yorum, değilse yanıt)
    public RecipeComment? ParentComment { get; set; } // Ana yorum
    public List<RecipeComment> Replies { get; set; } = new(); // Yanıtlar
    
    // Navigation properties
    public List<CommentLike> CommentLikes { get; set; } = new();
}

