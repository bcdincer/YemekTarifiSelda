using System.ComponentModel.DataAnnotations;

namespace BackendApi.Application.DTOs;

public class CreateCommentDto
{
    [Required(ErrorMessage = "Yorum içeriği gereklidir")]
    [StringLength(1000, MinimumLength = 1, ErrorMessage = "Yorum 1-1000 karakter arasında olmalıdır")]
    public string Content { get; set; } = string.Empty;
    
    public int? ParentCommentId { get; set; } // Yanıt verilecek yorum ID (null ise ana yorum)
}

