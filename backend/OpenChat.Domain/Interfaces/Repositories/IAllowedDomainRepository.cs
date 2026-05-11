using OpenChat.Domain.Entities;

namespace OpenChat.Domain.Interfaces.Repositories;

public interface IAllowedDomainRepository
{
    Task<List<AllowedDomain>> GetAllByUserAsync(string userId);
    Task<AllowedDomain?> GetByIdAsync(string id, string userId);
    Task<AllowedDomain?> GetByDomainAsync(string userId, string domain);
    Task<List<AllowedDomain>> GetEnabledByUserAsync(string userId);
    Task<AllowedDomain> CreateAsync(AllowedDomain domain);
    Task CreateManyAsync(IEnumerable<AllowedDomain> domains);
    Task<bool> UpdateAsync(string id, string userId, AllowedDomain domain);
    Task<bool> DeleteAsync(string id, string userId);
}
