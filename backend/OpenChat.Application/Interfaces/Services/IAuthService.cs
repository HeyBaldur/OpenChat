using OpenChat.Domain.Dtos;

namespace OpenChat.Application.Interfaces.Services;

public interface IAuthService
{
    Task<AuthResponse> SignupAsync(SignupRequest request);
    Task<AuthResponse> LoginAsync(LoginRequest request);
}
