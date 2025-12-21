using System.ComponentModel.DataAnnotations;

namespace BackendApi.Application.DTOs;

public class UpdateCollectionDto
{
    [Required(ErrorMessage = "Koleksiyon adı gereklidir")]
    [StringLength(100, MinimumLength = 1, ErrorMessage = "Koleksiyon adı 1-100 karakter arasında olmalıdır")]
    public string Name { get; set; } = string.Empty;

    [StringLength(500, ErrorMessage = "Açıklama en fazla 500 karakter olabilir")]
    public string? Description { get; set; }
}

