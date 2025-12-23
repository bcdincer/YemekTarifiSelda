namespace BackendApi.Domain.Interfaces;

public interface IUnitOfWork : IDisposable
{
    IRecipeRepository Recipes { get; }
    ICategoryRepository Categories { get; }
    IAuthorRepository Authors { get; }
    IBlogPostRepository BlogPosts { get; }
    IRatingRepository Ratings { get; }
    ILikeRepository Likes { get; }
    ICollectionRepository Collections { get; }
    ICollectionRecipeRepository CollectionRecipes { get; }
    ICommentRepository Comments { get; }
    ICommentLikeRepository CommentLikes { get; }
    IMealPlanRepository MealPlans { get; }
    IMealPlanItemRepository MealPlanItems { get; }
    IShoppingListRepository ShoppingLists { get; }
    IShoppingListItemRepository ShoppingListItems { get; }
    IRecipeImageRepository RecipeImages { get; }

    /// <summary>
    /// Tüm değişiklikleri kaydeder ve transaction'ı commit eder
    /// </summary>
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Transaction başlatır
    /// </summary>
    Task BeginTransactionAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Transaction'ı commit eder
    /// </summary>
    Task CommitTransactionAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Transaction'ı rollback eder
    /// </summary>
    Task RollbackTransactionAsync(CancellationToken cancellationToken = default);
}

