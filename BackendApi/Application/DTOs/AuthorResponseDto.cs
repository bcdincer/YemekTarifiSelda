namespace BackendApi.Application.DTOs;

public class AuthorResponseDto
{
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string? Bio { get; set; }
    public string? ProfileImageUrl { get; set; }
    public bool IsActive { get; set; }
    public int RecipeCount { get; set; } // Yazarın toplam tarif sayısı
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}

