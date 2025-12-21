using BackendApi.Domain.Interfaces;
using Microsoft.EntityFrameworkCore.Storage;

namespace BackendApi.Infrastructure.Persistence;

public class UnitOfWork : IUnitOfWork
{
    private readonly AppDbContext _context;
    private IDbContextTransaction? _transaction;
    private bool _disposed = false;

    private IRecipeRepository? _recipes;
    private ICategoryRepository? _categories;
    private IRatingRepository? _ratings;
    private ILikeRepository? _likes;
    private ICollectionRepository? _collections;
    private ICollectionRecipeRepository? _collectionRecipes;
    private ICommentRepository? _comments;
    private ICommentLikeRepository? _commentLikes;
    private IMealPlanRepository? _mealPlans;
    private IMealPlanItemRepository? _mealPlanItems;
    private IShoppingListRepository? _shoppingLists;
    private IShoppingListItemRepository? _shoppingListItems;

    public UnitOfWork(AppDbContext context)
    {
        _context = context;
    }

    public IRecipeRepository Recipes
    {
        get
        {
            return _recipes ??= new RecipeRepository(_context);
        }
    }

    public ICategoryRepository Categories
    {
        get
        {
            return _categories ??= new CategoryRepository(_context);
        }
    }

    public IRatingRepository Ratings
    {
        get
        {
            return _ratings ??= new RatingRepository(_context);
        }
    }

    public ILikeRepository Likes
    {
        get
        {
            return _likes ??= new LikeRepository(_context);
        }
    }

    public ICollectionRepository Collections
    {
        get
        {
            return _collections ??= new CollectionRepository(_context);
        }
    }

    public ICollectionRecipeRepository CollectionRecipes
    {
        get
        {
            return _collectionRecipes ??= new CollectionRecipeRepository(_context);
        }
    }

    public ICommentRepository Comments
    {
        get
        {
            return _comments ??= new CommentRepository(_context);
        }
    }

    public ICommentLikeRepository CommentLikes
    {
        get
        {
            return _commentLikes ??= new CommentLikeRepository(_context);
        }
    }

    public IMealPlanRepository MealPlans
    {
        get
        {
            return _mealPlans ??= new MealPlanRepository(_context);
        }
    }

    public IMealPlanItemRepository MealPlanItems
    {
        get
        {
            return _mealPlanItems ??= new MealPlanItemRepository(_context);
        }
    }

    public IShoppingListRepository ShoppingLists
    {
        get
        {
            return _shoppingLists ??= new ShoppingListRepository(_context);
        }
    }

    public IShoppingListItemRepository ShoppingListItems
    {
        get
        {
            return _shoppingListItems ??= new ShoppingListItemRepository(_context);
        }
    }

    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_transaction != null)
        {
            throw new InvalidOperationException("Transaction already started");
        }

        _transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
    }

    public async Task CommitTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_transaction == null)
        {
            throw new InvalidOperationException("No transaction to commit");
        }

        try
        {
            await _context.SaveChangesAsync(cancellationToken);
            await _transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await RollbackTransactionAsync(cancellationToken);
            throw;
        }
        finally
        {
            if (_transaction != null)
            {
                await _transaction.DisposeAsync();
                _transaction = null;
            }
        }
    }

    public async Task RollbackTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_transaction == null)
        {
            return;
        }

        try
        {
            await _transaction.RollbackAsync(cancellationToken);
        }
        finally
        {
            await _transaction.DisposeAsync();
            _transaction = null;
        }
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!_disposed && disposing)
        {
            _transaction?.Dispose();
            _context.Dispose();
            _disposed = true;
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
}

