using BackendApi.Application.DTOs;
using BackendApi.Application.Mappings;
using BackendApi.Domain.Entities;
using BackendApi.Domain.Interfaces;

namespace BackendApi.Application.Services;

public class AuthorService(IUnitOfWork unitOfWork, ILogger<AuthorService> logger) : IAuthorService
{
    private IAuthorRepository Repository => unitOfWork.Authors;

    public async Task<List<AuthorResponseDto>> GetAllAsync()
    {
        var authors = await Repository.GetAllAsync();
        return authors.Select(a => a.ToDto()).ToList();
    }

    public async Task<PagedResult<AuthorResponseDto>> GetAllPagedAsync(int pageNumber, int pageSize)
    {
        var (items, totalCount) = await Repository.GetAllPagedAsync(pageNumber, pageSize);
        var dtoItems = items.Select(a => a.ToDto()).ToList();
        return new PagedResult<AuthorResponseDto>(dtoItems, totalCount, pageNumber, pageSize);
    }

    public async Task<AuthorResponseDto?> GetByIdAsync(int id)
    {
        var author = await Repository.GetByIdAsync(id);
        return author?.ToDto();
    }

    public async Task<AuthorResponseDto?> GetByUserIdAsync(string userId)
    {
        var author = await Repository.GetByUserIdAsync(userId);
        return author?.ToDto();
    }

    public async Task<List<AuthorResponseDto>> GetActiveAuthorsAsync()
    {
        var authors = await Repository.GetActiveAuthorsAsync();
        return authors.Select(a => a.ToDto()).ToList();
    }

    public async Task<AuthorResponseDto> CreateAsync(CreateAuthorDto dto)
    {
        try
        {
            // Kullanıcı zaten yazar mı kontrol et
            var existingAuthor = await Repository.GetByUserIdAsync(dto.UserId);
            if (existingAuthor != null)
            {
                throw new InvalidOperationException($"User {dto.UserId} is already an author");
            }

            var author = dto.ToEntity();
            author.CreatedAt = DateTime.UtcNow;
            
            await Repository.AddAsync(author);
            await unitOfWork.SaveChangesAsync();
            
            logger.LogInformation("Author '{DisplayName}' created with id {AuthorId} for user {UserId}", 
                author.DisplayName, author.Id, author.UserId);
            
            var createdAuthor = await Repository.GetByIdAsync(author.Id);
            if (createdAuthor == null)
                throw new InvalidOperationException("Author was created but could not be retrieved");
            
            return createdAuthor.ToDto();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error creating author for user {UserId}", dto.UserId);
            throw;
        }
    }

    public async Task<bool> UpdateAsync(int id, UpdateAuthorDto dto)
    {
        try
        {
            var existing = await Repository.GetByIdAsync(id);
            if (existing == null)
            {
                logger.LogWarning("Author with id {AuthorId} not found for update", id);
                return false;
            }

            existing.UpdateEntity(dto);
            await Repository.UpdateAsync(existing);
            await unitOfWork.SaveChangesAsync();
            
            logger.LogInformation("Author {AuthorId} updated successfully", id);
            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error updating author {AuthorId}", id);
            throw;
        }
    }

    public async Task<bool> DeleteAsync(int id)
    {
        try
        {
            var author = await Repository.GetByIdAsync(id);
            if (author == null)
            {
                logger.LogWarning("Author with id {AuthorId} not found for deletion", id);
                return false;
            }

            await Repository.DeleteAsync(author);
            await unitOfWork.SaveChangesAsync();
            
            logger.LogInformation("Author {AuthorId} deleted successfully", id);
            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error deleting author {AuthorId}", id);
            throw;
        }
    }

    public async Task<bool> BecomeAuthorAsync(string userId, CreateAuthorDto dto)
    {
        try
        {
            // Kullanıcı zaten yazar mı kontrol et
            var existingAuthor = await Repository.GetByUserIdAsync(userId);
            if (existingAuthor != null)
            {
                // Zaten yazar, aktif et
                if (!existingAuthor.IsActive)
                {
                    existingAuthor.IsActive = true;
                    existingAuthor.UpdatedAt = DateTime.UtcNow;
                    await Repository.UpdateAsync(existingAuthor);
                    await unitOfWork.SaveChangesAsync();
                    logger.LogInformation("Author {AuthorId} reactivated for user {UserId}", existingAuthor.Id, userId);
                }
                return true;
            }

            // Yeni yazar oluştur
            dto.UserId = userId; // UserId'yi dto'dan al, ama güvenlik için parametre olarak gelen userId'yi kullan
            var author = dto.ToEntity();
            author.UserId = userId; // Güvenlik: Her zaman parametre olarak gelen userId'yi kullan
            author.CreatedAt = DateTime.UtcNow;
            
            await Repository.AddAsync(author);
            await unitOfWork.SaveChangesAsync();
            
            logger.LogInformation("User {UserId} became an author with id {AuthorId}", userId, author.Id);
            return true;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error making user {UserId} an author", userId);
            throw;
        }
    }
}

