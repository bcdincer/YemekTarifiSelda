namespace BackendApi.Domain.Events;

/// <summary>
/// Event raised when a recipe is created
/// </summary>
public class RecipeCreatedEvent : DomainEvent
{
    public int RecipeId { get; }
    public string RecipeTitle { get; }
    public string? UserEmail { get; }
    public DateTime CreatedAt { get; }

    public RecipeCreatedEvent(int recipeId, string recipeTitle, string? userEmail, DateTime createdAt)
    {
        RecipeId = recipeId;
        RecipeTitle = recipeTitle;
        UserEmail = userEmail;
        CreatedAt = createdAt;
    }
}

