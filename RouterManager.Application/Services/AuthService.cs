using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using RouterManager.Application.Interfaces;
using RouterManager.Domain.Entities;

namespace RouterManager.Application.Services;

public interface IAuthService
{
    Task<string?> RegisterAsync(string username, string password, CancellationToken ct = default);
    Task<string?> LoginAsync(string username, string password, CancellationToken ct = default);
}

public class AuthService : IAuthService
{
    private readonly IUserRepository _users;
    private readonly IConfiguration _config;

    public AuthService(IUserRepository users, IConfiguration config)
    {
        _users = users;
        _config = config;
    }

    public async Task<string?> RegisterAsync(string username, string password, CancellationToken ct = default)
    {
        var existing = await _users.GetByUsernameAsync(username, ct);
        if (existing != null) return null;
        var user = new User { Username = username, PasswordHash = BCrypt.Net.BCrypt.HashPassword(password) };
        await _users.AddAsync(user, ct);
        return GenerateJwt(user);
    }

    public async Task<string?> LoginAsync(string username, string password, CancellationToken ct = default)
    {
        var user = await _users.GetByUsernameAsync(username, ct);
        if (user == null) return null;
        if (!BCrypt.Net.BCrypt.Verify(password, user.PasswordHash)) return null;
        return GenerateJwt(user);
    }

    private string GenerateJwt(User user)
    {
        var key = _config["Jwt:Key"] ?? "dev-secret-key-change";
        var issuer = _config["Jwt:Issuer"] ?? "RouterManager";
        var creds = new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)), SecurityAlgorithms.HmacSha256);
        var claims = new[]
        {
            new Claim(ClaimTypes.Name, user.Username),
            new Claim(ClaimTypes.Role, user.Role)
        };
        var token = new JwtSecurityToken(issuer, null, claims, expires: DateTime.UtcNow.AddHours(4), signingCredentials: creds);
        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}