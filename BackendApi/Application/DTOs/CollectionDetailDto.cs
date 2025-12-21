using BackendApi.Application.DTOs;

namespace BackendApi.Application.DTOs;

public class CollectionDetailDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public List<RecipeResponseDto> Recipes { get; set; } = new();
}

