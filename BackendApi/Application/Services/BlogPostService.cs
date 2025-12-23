using BackendApi.Application.DTOs;
using BackendApi.Application.Mappings;
using BackendApi.Domain.Entities;
using BackendApi.Domain.Interfaces;

namespace BackendApi.Application.Services;

public class BlogPostService(IUnitOfWork unitOfWork, ILogger<BlogPostService> logger) : IBlogPostService
{
    private IBlogPostRepository Repository => unitOfWork.BlogPosts;

    public async Task<List<BlogPostResponseDto>> GetAllAsync()
    {
        var blogPosts = await Repository.GetAllAsync();
        return blogPosts.Select(b => b.ToDto()).ToList();
    }

    public async Task<PagedResult<BlogPostResponseDto>> GetAllPagedAsync(int pageNumber, int pageSize)
    {
        var (items, totalCount) = await Repository.GetAllPagedAsync(pageNumber, pageSize);
        var dtoItems = items.Select(b => b.ToDto()).ToList();
        return new PagedResult<BlogPostResponseDto>(dtoItems, totalCount, pageNumber, pageSize);
    }

    public async Task<List<BlogPostResponseDto>> GetPublishedAsync()
    {
        var blogPosts = await Repository.GetPublishedAsync();
        return blogPosts.Select(b => b.ToDto()).ToList();
    }

    public async Task<PagedResult<BlogPostResponseDto>> GetPublishedPagedAsync(int pageNumber, int pageSize)
    {
        var (items, totalCount) = await Repository.GetPublishedPagedAsync(pageNumber, pageSize);
        var dtoItems = items.Select(b => b.ToDto()).ToList();
        return new PagedResult<BlogPostResponseDto>(dtoItems, totalCount, pageNumber, pageSize);
    }

    public async Task<BlogPostResponseDto?> GetByIdAsync(int id)
    {
        var blogPost = await Repository.GetByIdAsync(id);
        return blogPost?.ToDto();
    }

    public async Task<List<BlogPostResponseDto>> GetByAuthorIdAsync(int authorId)
    {
        var blogPosts = await Repository.GetByAuthorIdAsync(authorId);
        return blogPosts.Select(b => b.ToDto()).ToList();
    }

    public async Task<PagedResult<BlogPostResponseDto>> GetByAuthorIdPagedAsync(int authorId, int pageNumber, int pageSize)
    {
        var (items, totalCount) = await Repository.GetByAuthorIdPagedAsync(authorId, pageNumber, pageSize);
        var dtoItems = items.Select(b => b.ToDto()).ToList();
        return new PagedResult<BlogPostResponseDto>(dtoItems, totalCount, pageNumber, pageSize);
    }

    public async Task<List<BlogPostResponseDto>> GetFeaturedAsync(int count = 6)
    {
        var blogPosts = await Repository.GetFeaturedAsync(count);
        return blogPosts.Select(b => b.ToDto()).ToList();
    }

    public async Task<List<BlogPostResponseDto>> GetRecentAsync(int count = 6)
    {
        var blogPosts = await Repository.GetRecentAsync(count);
        return blogPosts.Select(b => b.ToDto()).ToList();
    }

    public async Task<List<BlogPostResponseDto>> SearchAsync(string searchTerm)
    {
        var blogPosts = await Repository.SearchAsync(searchTerm);
        return blogPosts.Select(b => b.ToDto()).ToList();
    }

    public async Task<PagedResult<BlogPostResponseDto>> SearchPagedAsync(string searchTerm, int pageNumber, int pageSize)
    {
        var (items, totalCount) = await Repository.SearchPagedAsync(searchTerm, pageNumber, pageSize);
        var dtoItems = items.Select(b => b.ToDto()).ToList();
        return new PagedResult<BlogPostResponseDto>(dtoItems, totalCount, pageNumber, pageSize);
    }

    public async Task<BlogPostResponseDto> CreateAsync(CreateBlogPostDto dto)
    {
        try
        {
            var blogPost = dto.ToEntity();
            blogPost.CreatedAt = DateTime.UtcNow;
            
            await Repository.AddAsync(blogPost);
            await unitOfWork.SaveChangesAsync();
            
            logger.LogInformation("BlogPost '{Title}' created with id {BlogPostId}", blogPost.Title, blogPost.Id);
            
            var createdBlogPost = await Repository.GetByIdAsync(blogPost.Id);
            if (createdBlogPost == null)
                throw new InvalidOperationException("BlogPost was created but could not be retrieved");
            
            return createdBlogPost.ToDto();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error creating blog post '{Title}'", dto.Title);
            throw;
        }
    }

    public async Task<bool> UpdateAsync(int id, UpdateBlogPostDto dto)
    {
        try
        {
            var existing = await Repository.GetByIdAsync(id);
            if (existing == null)
            {
                logger.LogWarning("BlogPost with id {BlogPostId} not found for update", id);
                return false;
            }

            existing.UpdateEntity(dto);
            await Repository.UpdateAsync(existing);
            await unitOfWork.SaveChangesAsync();
            
            logger.LogInformation("BlogPost {BlogPostId} updated successfully", id);
            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error updating blog post {BlogPostId}", id);
            throw;
        }
    }

    public async Task<bool> DeleteAsync(int id)
    {
        try
        {
            var blogPost = await Repository.GetByIdAsync(id);
            if (blogPost == null)
            {
                logger.LogWarning("BlogPost with id {BlogPostId} not found for deletion", id);
                return false;
            }

            await Repository.DeleteAsync(blogPost);
            await unitOfWork.SaveChangesAsync();
            
            logger.LogInformation("BlogPost {BlogPostId} deleted successfully", id);
            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error deleting blog post {BlogPostId}", id);
            throw;
        }
    }

    public async Task IncrementViewCountAsync(int id)
    {
        try
        {
            var blogPost = await Repository.GetByIdAsync(id);
            if (blogPost != null)
            {
                blogPost.ViewCount++;
                await Repository.UpdateAsync(blogPost);
                await unitOfWork.SaveChangesAsync();
                logger.LogDebug("View count incremented for blog post {BlogPostId}", id);
            }
            else
            {
                logger.LogWarning("BlogPost with id {BlogPostId} not found for view count increment", id);
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error incrementing view count for blog post {BlogPostId}", id);
            // Don't throw - view count increment is not critical
        }
    }
}

