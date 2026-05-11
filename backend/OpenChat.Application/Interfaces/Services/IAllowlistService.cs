using OpenChat.Domain.Dtos;

namespace OpenChat.Application.Interfaces.Services;

public interface IAllowlistService
{
    Task<List<AllowedDomainResponse>> GetAllForUserAsync(string userId);
    Task<AllowedDomainResponse?> GetByIdAsync(string id, string userId);
    Task<AllowedDomainResponse> CreateAsync(AllowedDomainRequest request, string userId);
    Task<AllowedDomainResponse> UpdateAsync(string id, AllowedDomainRequest request, string userId);
    Task DeleteAsync(string id, string userId);
    Task<AllowedDomainResponse> ToggleAsync(string id, string userId);
    Task<bool> IsDomainAllowedAsync(string urlOrDomain, string userId);
}
