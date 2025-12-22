namespace BackendApi.Application.DTOs;

public class AdjustIngredientsRequestDto
{
    public List<string> Ingredients { get; set; } = new();
    public int OriginalServings { get; set; }
    public int NewServings { get; set; }
}

