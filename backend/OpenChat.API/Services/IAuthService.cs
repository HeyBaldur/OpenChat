using OpenChat.API.Models;

namespace OpenChat.API.Services;

public interface IAuthService
{
    Task<AuthResponse> SignupAsync(SignupRequest request);
    Task<AuthResponse> LoginAsync(LoginRequest request);
}
