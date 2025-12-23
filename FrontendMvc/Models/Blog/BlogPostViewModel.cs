using System.ComponentModel.DataAnnotations;
using FrontendMvc.Models.Recipes;

namespace FrontendMvc.Models.Blog;

public class BlogPostViewModel
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public string Excerpt { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
    public string? ImageBanner { get; set; }
    public int ViewCount { get; set; }
    public int LikeCount { get; set; }
    public int CommentCount { get; set; }
    public bool IsPublished { get; set; }
    public bool IsFeatured { get; set; }
    public int? AuthorId { get; set; }
    public AuthorViewModel? Author { get; set; }
    public int? CategoryId { get; set; }
    public CategoryViewModel? Category { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public DateTime? PublishedAt { get; set; }
}

public class CreateBlogPostViewModel
{
    [Required(ErrorMessage = "Başlık zorunludur")]
    [StringLength(200, ErrorMessage = "Başlık en fazla 200 karakter olabilir")]
    [Display(Name = "Başlık")]
    public string Title { get; set; } = string.Empty;

    [Required(ErrorMessage = "İçerik zorunludur")]
    [Display(Name = "İçerik")]
    public string Content { get; set; } = string.Empty;

    [Required(ErrorMessage = "Özet zorunludur")]
    [StringLength(500, ErrorMessage = "Özet en fazla 500 karakter olabilir")]
    [Display(Name = "Özet")]
    public string Excerpt { get; set; } = string.Empty;

    [Url(ErrorMessage = "Geçerli bir URL giriniz")]
    [Display(Name = "Görsel URL")]
    public string? ImageUrl { get; set; }

    [StringLength(200, ErrorMessage = "Banner metni en fazla 200 karakter olabilir")]
    [Display(Name = "Görsel Banner Metni")]
    public string? ImageBanner { get; set; }

    [Display(Name = "Yayınla")]
    public bool IsPublished { get; set; } = true;

    [Display(Name = "Öne Çıkan")]
    public bool IsFeatured { get; set; } = false;

    [Display(Name = "Kategori")]
    public int? CategoryId { get; set; }
}

