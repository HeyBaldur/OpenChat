using OpenChat.API.Models;

namespace OpenChat.API.Repositories;

public interface IUserRepository
{
    Task<User?> FindByEmailAsync(string email);
    Task CreateAsync(User user);
}
