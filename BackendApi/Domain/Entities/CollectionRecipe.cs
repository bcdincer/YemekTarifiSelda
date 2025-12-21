namespace BackendApi.Domain.Entities;

public class CollectionRecipe
{
    public int Id { get; set; }
    public int CollectionId { get; set; }
    public Collection Collection { get; set; } = null!;
    public int RecipeId { get; set; }
    public Recipe Recipe { get; set; } = null!;
    public DateTime AddedAt { get; set; } = DateTime.UtcNow;
}

