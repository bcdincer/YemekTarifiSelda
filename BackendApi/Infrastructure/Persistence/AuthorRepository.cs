using BackendApi.Domain.Entities;
using BackendApi.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace BackendApi.Infrastructure.Persistence;

public class AuthorRepository(AppDbContext context) : IAuthorRepository
{
    private readonly AppDbContext _context = context;

    public async Task<Author?> GetByIdAsync(int id)
        => await _context.Authors
            .Include(a => a.Recipes)
            .FirstOrDefaultAsync(a => a.Id == id);

    public async Task<Author?> GetByUserIdAsync(string userId)
        => await _context.Authors
            .Include(a => a.Recipes)
            .FirstOrDefaultAsync(a => a.UserId == userId);

    public async Task<List<Author>> GetAllAsync()
        => await _context.Authors
            .Include(a => a.Recipes)
            .OrderByDescending(a => a.CreatedAt)
            .ToListAsync();

    public async Task<(List<Author> Items, int TotalCount)> GetAllPagedAsync(int pageNumber, int pageSize)
    {
        var query = _context.Authors
            .Include(a => a.Recipes)
            .OrderByDescending(a => a.CreatedAt);
        
        var totalCount = await query.CountAsync();
        
        var items = await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
        
        return (items, totalCount);
    }

    public async Task<List<Author>> GetActiveAuthorsAsync()
        => await _context.Authors
            .Include(a => a.Recipes)
            .Where(a => a.IsActive)
            .OrderByDescending(a => a.CreatedAt)
            .ToListAsync();

    public async Task AddAsync(Author author)
    {
        await _context.Authors.AddAsync(author);
    }

    public Task UpdateAsync(Author author)
    {
        _context.Authors.Update(author);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(Author author)
    {
        _context.Authors.Remove(author);
        return Task.CompletedTask;
    }
}

