using OpenChat.Domain.Entities;

namespace OpenChat.Domain.Interfaces.Repositories;

public interface IUserRepository
{
    Task<User?> FindByEmailAsync(string email);
    Task CreateAsync(User user);
}
