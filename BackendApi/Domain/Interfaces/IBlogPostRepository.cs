using BackendApi.Domain.Entities;

namespace BackendApi.Domain.Interfaces;

public interface IBlogPostRepository
{
    Task<List<BlogPost>> GetAllAsync();
    Task<(List<BlogPost> Items, int TotalCount)> GetAllPagedAsync(int pageNumber, int pageSize);
    Task<List<BlogPost>> GetPublishedAsync();
    Task<(List<BlogPost> Items, int TotalCount)> GetPublishedPagedAsync(int pageNumber, int pageSize);
    Task<BlogPost?> GetByIdAsync(int id);
    Task<List<BlogPost>> GetByAuthorIdAsync(int authorId);
    Task<(List<BlogPost> Items, int TotalCount)> GetByAuthorIdPagedAsync(int authorId, int pageNumber, int pageSize);
    Task<List<BlogPost>> GetFeaturedAsync(int count = 6);
    Task<List<BlogPost>> GetRecentAsync(int count = 6);
    Task<List<BlogPost>> SearchAsync(string searchTerm);
    Task<(List<BlogPost> Items, int TotalCount)> SearchPagedAsync(string searchTerm, int pageNumber, int pageSize);
    Task AddAsync(BlogPost blogPost);
    Task UpdateAsync(BlogPost blogPost);
    Task DeleteAsync(BlogPost blogPost);
}

