namespace BackendApi.Domain.Entities;

public class RecipeRating
{
    public int Id { get; set; }
    public int RecipeId { get; set; }
    public Recipe Recipe { get; set; } = null!;
    public string UserId { get; set; } = string.Empty; // Kullanıcı ID (Identity UserId veya IP)
    public int Rating { get; set; } // 1-5 arası puan
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}

