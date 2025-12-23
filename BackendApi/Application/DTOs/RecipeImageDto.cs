namespace BackendApi.Application.DTOs;

public class RecipeImageDto
{
    public int Id { get; set; }
    public int RecipeId { get; set; }
    public string ImageUrl { get; set; } = string.Empty;
    public bool IsPrimary { get; set; } = false;
    public int DisplayOrder { get; set; } = 0;
    public DateTime CreatedAt { get; set; }
}

