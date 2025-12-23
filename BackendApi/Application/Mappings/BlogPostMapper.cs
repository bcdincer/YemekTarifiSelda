using BackendApi.Application.DTOs;
using BackendApi.Domain.Entities;

namespace BackendApi.Application.Mappings;

public static class BlogPostMapper
{
    public static BlogPost ToEntity(this CreateBlogPostDto dto)
    {
        return new BlogPost
        {
            Title = dto.Title,
            Content = dto.Content,
            Excerpt = dto.Excerpt,
            ImageUrl = dto.ImageUrl,
            ImageBanner = dto.ImageBanner,
            IsPublished = dto.IsPublished,
            IsFeatured = dto.IsFeatured,
            AuthorId = dto.AuthorId,
            CategoryId = dto.CategoryId,
            PublishedAt = dto.IsPublished ? DateTime.UtcNow : null
        };
    }

    public static void UpdateEntity(this BlogPost existing, UpdateBlogPostDto dto)
    {
        existing.Title = dto.Title;
        existing.Content = dto.Content;
        existing.Excerpt = dto.Excerpt;
        existing.ImageUrl = dto.ImageUrl;
        existing.ImageBanner = dto.ImageBanner;
        existing.IsPublished = dto.IsPublished;
        existing.IsFeatured = dto.IsFeatured;
        existing.CategoryId = dto.CategoryId;
        existing.UpdatedAt = DateTime.UtcNow;
        
        // Eğer yayınlanıyorsa ve daha önce yayınlanmamışsa, PublishedAt'i set et
        if (dto.IsPublished && !existing.PublishedAt.HasValue)
        {
            existing.PublishedAt = DateTime.UtcNow;
        }
    }

    public static BlogPostResponseDto ToDto(this BlogPost blogPost)
    {
        return new BlogPostResponseDto
        {
            Id = blogPost.Id,
            Title = blogPost.Title,
            Content = blogPost.Content,
            Excerpt = blogPost.Excerpt,
            ImageUrl = blogPost.ImageUrl,
            ImageBanner = blogPost.ImageBanner,
            ViewCount = blogPost.ViewCount,
            LikeCount = blogPost.LikeCount,
            CommentCount = blogPost.CommentCount,
            IsPublished = blogPost.IsPublished,
            IsFeatured = blogPost.IsFeatured,
            AuthorId = blogPost.AuthorId,
            Author = blogPost.Author != null ? new AuthorDto
            {
                Id = blogPost.Author.Id,
                UserId = blogPost.Author.UserId,
                DisplayName = blogPost.Author.DisplayName,
                Bio = blogPost.Author.Bio,
                ProfileImageUrl = blogPost.Author.ProfileImageUrl
            } : null,
            CategoryId = blogPost.CategoryId,
            Category = blogPost.Category != null ? new CategoryDto
            {
                Id = blogPost.Category.Id,
                Name = blogPost.Category.Name,
                Description = blogPost.Category.Description,
                Icon = blogPost.Category.Icon
            } : null,
            CreatedAt = blogPost.CreatedAt,
            UpdatedAt = blogPost.UpdatedAt,
            PublishedAt = blogPost.PublishedAt
        };
    }
}

