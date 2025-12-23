namespace BackendApi.Domain.Entities;

public class Author
{
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty; // Identity UserId (string)
    public string DisplayName { get; set; } = string.Empty; // Yazarın görünen adı
    public string? Bio { get; set; } // Yazar hakkında kısa bilgi
    public string? ProfileImageUrl { get; set; } // Profil fotoğrafı
    public bool IsActive { get; set; } = true; // Yazar aktif mi?
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    
    // Navigation property
    public List<Recipe> Recipes { get; set; } = new();
    public List<BlogPost> BlogPosts { get; set; } = new();
}

