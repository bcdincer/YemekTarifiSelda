using BackendApi.Application.DTOs;
using BackendApi.Domain.Entities;

namespace BackendApi.Application.Services;

public interface ICategoryService
{
    Task<List<CategoryResponseDto>> GetAllAsync();
    Task<CategoryResponseDto?> GetByIdAsync(int id);
    Task<CategoryResponseDto> CreateAsync(Category category);
    Task<bool> UpdateAsync(int id, Category updated);
    Task<bool> DeleteAsync(int id);
}
