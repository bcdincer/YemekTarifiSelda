using BackendApi.Application.DTOs;
using BackendApi.Domain.Entities;

namespace BackendApi.Application.Services;

public interface IBlogPostService
{
    Task<List<BlogPostResponseDto>> GetAllAsync();
    Task<PagedResult<BlogPostResponseDto>> GetAllPagedAsync(int pageNumber, int pageSize);
    Task<List<BlogPostResponseDto>> GetPublishedAsync();
    Task<PagedResult<BlogPostResponseDto>> GetPublishedPagedAsync(int pageNumber, int pageSize);
    Task<BlogPostResponseDto?> GetByIdAsync(int id);
    Task<List<BlogPostResponseDto>> GetByAuthorIdAsync(int authorId);
    Task<PagedResult<BlogPostResponseDto>> GetByAuthorIdPagedAsync(int authorId, int pageNumber, int pageSize);
    Task<List<BlogPostResponseDto>> GetFeaturedAsync(int count = 6);
    Task<List<BlogPostResponseDto>> GetRecentAsync(int count = 6);
    Task<List<BlogPostResponseDto>> SearchAsync(string searchTerm);
    Task<PagedResult<BlogPostResponseDto>> SearchPagedAsync(string searchTerm, int pageNumber, int pageSize);
    Task<BlogPostResponseDto> CreateAsync(CreateBlogPostDto dto);
    Task<bool> UpdateAsync(int id, UpdateBlogPostDto dto);
    Task<bool> DeleteAsync(int id);
    Task IncrementViewCountAsync(int id);
}

