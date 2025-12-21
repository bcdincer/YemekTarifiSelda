namespace BackendApi.Domain.Entities;

public class CommentLike
{
    public int Id { get; set; }
    public int CommentId { get; set; }
    public RecipeComment Comment { get; set; } = null!;
    public string UserId { get; set; } = string.Empty; // Kullanıcı ID
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

