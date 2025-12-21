using BackendApi.Application.DTOs;
using BackendApi.Domain.Entities;

namespace BackendApi.Application.Mappings;

public static class CategoryMapper
{
    public static Category ToEntity(this CreateCategoryDto dto)
    {
        return new Category
        {
            Name = dto.Name,
            Description = dto.Description,
            Icon = dto.Icon,
            DisplayOrder = dto.DisplayOrder
        };
    }

    public static void UpdateEntity(this Category existing, CreateCategoryDto dto)
    {
        existing.Name = dto.Name;
        existing.Description = dto.Description;
        existing.Icon = dto.Icon;
        existing.DisplayOrder = dto.DisplayOrder;
    }

    public static CategoryResponseDto ToDto(this Category category)
    {
        return new CategoryResponseDto
        {
            Id = category.Id,
            Name = category.Name,
            Description = category.Description,
            Icon = category.Icon,
            DisplayOrder = category.DisplayOrder,
            CreatedAt = category.CreatedAt
        };
    }
}

