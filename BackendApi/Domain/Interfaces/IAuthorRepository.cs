using BackendApi.Domain.Entities;

namespace BackendApi.Domain.Interfaces;

public interface IAuthorRepository
{
    Task<Author?> GetByIdAsync(int id);
    Task<Author?> GetByUserIdAsync(string userId);
    Task<List<Author>> GetAllAsync();
    Task<(List<Author> Items, int TotalCount)> GetAllPagedAsync(int pageNumber, int pageSize);
    Task<List<Author>> GetActiveAuthorsAsync();
    Task AddAsync(Author author);
    Task UpdateAsync(Author author);
    Task DeleteAsync(Author author);
}

