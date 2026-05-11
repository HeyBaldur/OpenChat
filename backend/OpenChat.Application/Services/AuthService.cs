using OpenChat.Application.Interfaces.External;
using OpenChat.Application.Interfaces.Services;
using OpenChat.Domain.Dtos;
using OpenChat.Domain.Entities;
using OpenChat.Domain.Interfaces.Repositories;
using System.Text.RegularExpressions;

namespace OpenChat.Application.Services;

public class AuthService : IAuthService
{
    private readonly IUserRepository _userRepo;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtTokenGenerator _jwtGenerator;

    public AuthService(IUserRepository userRepo, IPasswordHasher passwordHasher, IJwtTokenGenerator jwtGenerator)
    {
        _userRepo = userRepo;
        _passwordHasher = passwordHasher;
        _jwtGenerator = jwtGenerator;
    }

    public async Task<AuthResponse> SignupAsync(SignupRequest request)
    {
        if (!IsValidEmail(request.Email))
            throw new ArgumentException("Invalid email format.");

        if (!IsValidPassword(request.Password))
            throw new ArgumentException("Password must be at least 8 characters and contain at least one letter and one number.");

        if (await _userRepo.FindByEmailAsync(request.Email) is not null)
            throw new InvalidOperationException("An account with this email already exists.");

        var user = new User
        {
            Email = request.Email.Trim().ToLowerInvariant(),
            PasswordHash = _passwordHasher.Hash(request.Password),
            Role = "user"
        };

        await _userRepo.CreateAsync(user);
        return BuildResponse(user);
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request)
    {
        var user = await _userRepo.FindByEmailAsync(request.Email)
            ?? throw new UnauthorizedAccessException("Invalid email or password.");

        if (!_passwordHasher.Verify(request.Password, user.PasswordHash))
            throw new UnauthorizedAccessException("Invalid email or password.");

        return BuildResponse(user);
    }

    private AuthResponse BuildResponse(User user) => new()
    {
        Token = _jwtGenerator.Generate(user),
        UserId = user.Id!,
        Email = user.Email,
        Role = user.Role
    };

    private static bool IsValidEmail(string email) =>
        !string.IsNullOrWhiteSpace(email) &&
        Regex.IsMatch(email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$");

    private static bool IsValidPassword(string password) =>
        password.Length >= 8 &&
        password.Any(char.IsLetter) &&
        password.Any(char.IsDigit);
}
