using BackendApi.Domain.Entities;
using BackendApi.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace BackendApi.Infrastructure.Persistence;

public class BlogPostRepository(AppDbContext context) : IBlogPostRepository
{
    private readonly AppDbContext _context = context;

    public async Task<List<BlogPost>> GetAllAsync()
        => await _context.BlogPosts
            .Include(b => b.Author)
            .Include(b => b.Category)
            .OrderByDescending(b => b.CreatedAt)
            .ToListAsync();

    public async Task<(List<BlogPost> Items, int TotalCount)> GetAllPagedAsync(int pageNumber, int pageSize)
    {
        var query = _context.BlogPosts
            .Include(b => b.Author)
            .Include(b => b.Category)
            .OrderByDescending(b => b.CreatedAt);
        
        var totalCount = await query.CountAsync();
        
        var items = await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
        
        return (items, totalCount);
    }

    public async Task<List<BlogPost>> GetPublishedAsync()
        => await _context.BlogPosts
            .Include(b => b.Author)
            .Include(b => b.Category)
            .Where(b => b.IsPublished)
            .OrderByDescending(b => b.PublishedAt ?? b.CreatedAt)
            .ToListAsync();

    public async Task<(List<BlogPost> Items, int TotalCount)> GetPublishedPagedAsync(int pageNumber, int pageSize)
    {
        var query = _context.BlogPosts
            .Include(b => b.Author)
            .Include(b => b.Category)
            .Where(b => b.IsPublished)
            .OrderByDescending(b => b.PublishedAt ?? b.CreatedAt);
        
        var totalCount = await query.CountAsync();
        
        var items = await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
        
        return (items, totalCount);
    }

    public async Task<BlogPost?> GetByIdAsync(int id)
        => await _context.BlogPosts
            .Include(b => b.Author)
            .Include(b => b.Category)
            .FirstOrDefaultAsync(b => b.Id == id);

    public async Task<List<BlogPost>> GetByAuthorIdAsync(int authorId)
        => await _context.BlogPosts
            .Include(b => b.Author)
            .Include(b => b.Category)
            .Where(b => b.AuthorId == authorId && b.IsPublished)
            .OrderByDescending(b => b.PublishedAt ?? b.CreatedAt)
            .ToListAsync();

    public async Task<(List<BlogPost> Items, int TotalCount)> GetByAuthorIdPagedAsync(int authorId, int pageNumber, int pageSize)
    {
        var query = _context.BlogPosts
            .Include(b => b.Author)
            .Include(b => b.Category)
            .Where(b => b.AuthorId == authorId && b.IsPublished)
            .OrderByDescending(b => b.PublishedAt ?? b.CreatedAt);
        
        var totalCount = await query.CountAsync();
        
        var items = await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
        
        return (items, totalCount);
    }

    public async Task<List<BlogPost>> GetFeaturedAsync(int count = 6)
        => await _context.BlogPosts
            .Include(b => b.Author)
            .Include(b => b.Category)
            .Where(b => b.IsPublished && b.IsFeatured)
            .OrderByDescending(b => b.PublishedAt ?? b.CreatedAt)
            .Take(count)
            .ToListAsync();

    public async Task<List<BlogPost>> GetRecentAsync(int count = 6)
        => await _context.BlogPosts
            .Include(b => b.Author)
            .Include(b => b.Category)
            .Where(b => b.IsPublished)
            .OrderByDescending(b => b.PublishedAt ?? b.CreatedAt)
            .Take(count)
            .ToListAsync();

    public async Task<List<BlogPost>> SearchAsync(string searchTerm)
    {
        var term = searchTerm.ToLower();
        return await _context.BlogPosts
            .Include(b => b.Author)
            .Include(b => b.Category)
            .Where(b => b.IsPublished && (
                b.Title.ToLower().Contains(term) ||
                b.Content.ToLower().Contains(term) ||
                b.Excerpt.ToLower().Contains(term) ||
                (b.Author != null && b.Author.DisplayName.ToLower().Contains(term)) ||
                (b.Category != null && b.Category.Name.ToLower().Contains(term))
            ))
            .OrderByDescending(b => b.PublishedAt ?? b.CreatedAt)
            .ToListAsync();
    }

    public async Task<(List<BlogPost> Items, int TotalCount)> SearchPagedAsync(string searchTerm, int pageNumber, int pageSize)
    {
        var term = searchTerm.ToLower();
        var query = _context.BlogPosts
            .Include(b => b.Author)
            .Include(b => b.Category)
            .Where(b => b.IsPublished && (
                b.Title.ToLower().Contains(term) ||
                b.Content.ToLower().Contains(term) ||
                b.Excerpt.ToLower().Contains(term) ||
                (b.Author != null && b.Author.DisplayName.ToLower().Contains(term)) ||
                (b.Category != null && b.Category.Name.ToLower().Contains(term))
            ))
            .OrderByDescending(b => b.PublishedAt ?? b.CreatedAt);
        
        var totalCount = await query.CountAsync();
        
        var items = await query
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();
        
        return (items, totalCount);
    }

    public async Task AddAsync(BlogPost blogPost)
    {
        await _context.BlogPosts.AddAsync(blogPost);
    }

    public Task UpdateAsync(BlogPost blogPost)
    {
        _context.BlogPosts.Update(blogPost);
        return Task.CompletedTask;
    }

    public Task DeleteAsync(BlogPost blogPost)
    {
        _context.BlogPosts.Remove(blogPost);
        return Task.CompletedTask;
    }
}

