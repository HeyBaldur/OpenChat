using Microsoft.IdentityModel.Tokens;
using OpenChat.API.Models;
using OpenChat.API.Repositories;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Text.RegularExpressions;

namespace OpenChat.API.Services;

public class AuthService : IAuthService
{
    private readonly IUserRepository _userRepo;
    private readonly IConfiguration _config;

    public AuthService(IUserRepository userRepo, IConfiguration config)
    {
        _userRepo = userRepo;
        _config = config;
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
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password, workFactor: 12),
            Role = "user"
        };

        await _userRepo.CreateAsync(user);
        return BuildResponse(user);
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request)
    {
        var user = await _userRepo.FindByEmailAsync(request.Email)
            ?? throw new UnauthorizedAccessException("Invalid email or password.");

        if (!BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            throw new UnauthorizedAccessException("Invalid email or password.");

        return BuildResponse(user);
    }

    private AuthResponse BuildResponse(User user) => new()
    {
        Token = GenerateToken(user),
        UserId = user.Id!,
        Email = user.Email,
        Role = user.Role
    };

    private string GenerateToken(User user)
    {
        var secret = _config["Jwt:Secret"]
            ?? throw new InvalidOperationException("JWT secret not configured.");

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
        var expiry = int.TryParse(_config["Jwt:ExpiryDays"], out var d) ? d : 7;

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id!),
            new Claim(ClaimTypes.Email, user.Email),
            new Claim(ClaimTypes.Role, user.Role)
        };

        var token = new JwtSecurityToken(
            claims: claims,
            expires: DateTime.UtcNow.AddDays(expiry),
            signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256));

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private static bool IsValidEmail(string email) =>
        !string.IsNullOrWhiteSpace(email) &&
        Regex.IsMatch(email, @"^[^@\s]+@[^@\s]+\.[^@\s]+$");

    private static bool IsValidPassword(string password) =>
        password.Length >= 8 &&
        password.Any(char.IsLetter) &&
        password.Any(char.IsDigit);
}
