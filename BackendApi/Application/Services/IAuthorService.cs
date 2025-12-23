using BackendApi.Application.DTOs;
using BackendApi.Domain.Entities;

namespace BackendApi.Application.Services;

public interface IAuthorService
{
    Task<List<AuthorResponseDto>> GetAllAsync();
    Task<PagedResult<AuthorResponseDto>> GetAllPagedAsync(int pageNumber, int pageSize);
    Task<AuthorResponseDto?> GetByIdAsync(int id);
    Task<AuthorResponseDto?> GetByUserIdAsync(string userId);
    Task<List<AuthorResponseDto>> GetActiveAuthorsAsync();
    Task<AuthorResponseDto> CreateAsync(CreateAuthorDto dto);
    Task<bool> UpdateAsync(int id, UpdateAuthorDto dto);
    Task<bool> DeleteAsync(int id);
    Task<bool> BecomeAuthorAsync(string userId, CreateAuthorDto dto);
}

