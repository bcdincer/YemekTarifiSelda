using System.ComponentModel.DataAnnotations;

namespace FrontendMvc.Models.Recipes;

public class AuthorViewModel
{
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string? Bio { get; set; }
    public string? ProfileImageUrl { get; set; }
    public bool IsActive { get; set; }
    public int RecipeCount { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

public class BecomeAuthorViewModel
{
    [Required(ErrorMessage = "Görünen ad zorunludur")]
    [StringLength(100, ErrorMessage = "Görünen ad en fazla 100 karakter olabilir")]
    [Display(Name = "Görünen Ad")]
    public string DisplayName { get; set; } = string.Empty;

    [StringLength(500, ErrorMessage = "Biyografi en fazla 500 karakter olabilir")]
    [Display(Name = "Biyografi")]
    public string? Bio { get; set; }

    [Url(ErrorMessage = "Geçerli bir URL giriniz")]
    [Display(Name = "Profil Fotoğrafı URL")]
    public string? ProfileImageUrl { get; set; }
}

