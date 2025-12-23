namespace BackendApi.Domain.Entities;

public class BlogPost
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty; // HTML içerik
    public string Excerpt { get; set; } = string.Empty; // Kısa özet (liste görünümü için)
    public string? ImageUrl { get; set; } // Blog görseli
    public string? ImageBanner { get; set; } // Görsel üzerindeki banner metni
    
    // İstatistikler
    public int ViewCount { get; set; } = 0;
    public int LikeCount { get; set; } = 0;
    public int CommentCount { get; set; } = 0;
    
    // Durum
    public bool IsPublished { get; set; } = true; // Yayınlandı mı?
    public bool IsFeatured { get; set; } = false; // Öne çıkan mı?
    
    // İlişkiler
    public int? AuthorId { get; set; }
    public Author? Author { get; set; }
    
    public int? CategoryId { get; set; }
    public Category? Category { get; set; }
    
    // Tarih bilgileri
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public DateTime? PublishedAt { get; set; } // Yayınlanma tarihi
}

