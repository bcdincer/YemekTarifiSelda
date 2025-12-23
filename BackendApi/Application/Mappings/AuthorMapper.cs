using BackendApi.Application.DTOs;
using BackendApi.Domain.Entities;

namespace BackendApi.Application.Mappings;

public static class AuthorMapper
{
    public static Author ToEntity(this CreateAuthorDto dto)
    {
        return new Author
        {
            UserId = dto.UserId,
            DisplayName = dto.DisplayName,
            Bio = dto.Bio,
            ProfileImageUrl = dto.ProfileImageUrl,
            IsActive = true
        };
    }

    public static void UpdateEntity(this Author existing, UpdateAuthorDto dto)
    {
        existing.DisplayName = dto.DisplayName;
        existing.Bio = dto.Bio;
        existing.ProfileImageUrl = dto.ProfileImageUrl;
        existing.IsActive = dto.IsActive;
        existing.UpdatedAt = DateTime.UtcNow;
    }

    public static AuthorResponseDto ToDto(this Author author)
    {
        return new AuthorResponseDto
        {
            Id = author.Id,
            UserId = author.UserId,
            DisplayName = author.DisplayName,
            Bio = author.Bio,
            ProfileImageUrl = author.ProfileImageUrl,
            IsActive = author.IsActive,
            RecipeCount = author.Recipes?.Count ?? 0,
            CreatedAt = author.CreatedAt,
            UpdatedAt = author.UpdatedAt
        };
    }
}

