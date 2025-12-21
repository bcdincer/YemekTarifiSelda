using BackendApi.Application.DTOs;
using BackendApi.Application.Mappings;
using BackendApi.Domain.Entities;
using BackendApi.Domain.Interfaces;

namespace BackendApi.Application.Services;

public class CategoryService(IUnitOfWork unitOfWork, ILogger<CategoryService> logger) : ICategoryService
{
    private ICategoryRepository Repository => unitOfWork.Categories;

    public async Task<List<CategoryResponseDto>> GetAllAsync()
    {
        var categories = await Repository.GetAllAsync();
        return categories.Select(c => c.ToDto()).ToList();
    }

    public async Task<CategoryResponseDto?> GetByIdAsync(int id)
    {
        var category = await Repository.GetByIdAsync(id);
        return category?.ToDto();
    }

    public async Task<CategoryResponseDto> CreateAsync(Category category)
    {
        try
        {
            category.CreatedAt = DateTime.UtcNow;
            await Repository.AddAsync(category);
            await unitOfWork.SaveChangesAsync();
            logger.LogInformation("Category '{CategoryName}' created with id {CategoryId}", category.Name, category.Id);
            
            var createdCategory = await Repository.GetByIdAsync(category.Id);
            if (createdCategory == null)
                throw new InvalidOperationException("Category was created but could not be retrieved");
            
            return createdCategory.ToDto();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error creating category '{CategoryName}'", category?.Name);
            throw;
        }
    }

    public async Task<bool> UpdateAsync(int id, Category updated)
    {
        try
        {
            var existing = await Repository.GetByIdAsync(id);
            if (existing == null)
            {
                logger.LogWarning("Category with id {CategoryId} not found for update", id);
                return false;
            }

            existing.Name = updated.Name;
            existing.Description = updated.Description;
            existing.Icon = updated.Icon;
            existing.DisplayOrder = updated.DisplayOrder;

            await Repository.UpdateAsync(existing);
            await unitOfWork.SaveChangesAsync();
            logger.LogInformation("Category {CategoryId} updated successfully", id);
            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error updating category {CategoryId}", id);
            throw;
        }
    }

    public async Task<bool> DeleteAsync(int id)
    {
        try
        {
            var category = await Repository.GetByIdAsync(id);
            if (category == null)
            {
                logger.LogWarning("Category with id {CategoryId} not found for deletion", id);
                return false;
            }

            await Repository.DeleteAsync(category);
            await unitOfWork.SaveChangesAsync();
            logger.LogInformation("Category {CategoryId} deleted successfully", id);
            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error deleting category {CategoryId}", id);
            throw;
        }
    }
}

