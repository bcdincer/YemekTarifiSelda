using BackendApi.Domain.Entities;

namespace BackendApi.Domain.Interfaces;

public interface ICollectionRepository
{
    Task<Collection?> GetByIdAsync(int id);
    Task<List<Collection>> GetByUserIdAsync(string userId);
    Task<Collection> AddAsync(Collection collection);
    Task UpdateAsync(Collection collection);
    Task DeleteAsync(Collection collection);
    Task<bool> ExistsAsync(int id, string userId); // Kullanıcının koleksiyonu var mı kontrol et
    Task<bool> NameExistsForUserAsync(string name, string userId, int? excludeId = null); // Aynı isimde koleksiyon var mı
    Task SaveChangesAsync();
}

