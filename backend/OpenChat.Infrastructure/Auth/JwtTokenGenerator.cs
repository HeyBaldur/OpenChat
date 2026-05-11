using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using OpenChat.Application.Interfaces.External;
using OpenChat.Domain.Entities;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace OpenChat.Infrastructure.Auth;

public class JwtTokenGenerator : IJwtTokenGenerator
{
    private readonly IConfiguration _config;

    public JwtTokenGenerator(IConfiguration config)
    {
        _config = config;
    }

    public string Generate(User user)
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
}
