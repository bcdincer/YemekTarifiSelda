namespace BackendApi.Domain.Interfaces;

public interface IUnitOfWork : IDisposable
{
    IRecipeRepository Recipes { get; }
    ICategoryRepository Categories { get; }
    IRatingRepository Ratings { get; }
    ILikeRepository Likes { get; }

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

