namespace BackendApi.Domain.Entities;

public class RecipeLike
{
    public int Id { get; set; }
    public int RecipeId { get; set; }
    public Recipe Recipe { get; set; } = null!;
    public string UserId { get; set; } = string.Empty; // Kullanıcı ID (Identity UserId veya IP)
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}

