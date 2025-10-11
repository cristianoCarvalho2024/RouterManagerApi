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
    private readonly ITokenStore _tokenStore;

    public AuthService(IUserRepository users, IConfiguration config, ITokenStore tokenStore)
    {
        _users = users;
        _config = config;
        _tokenStore = tokenStore;
    }

    public async Task<string?> RegisterAsync(string username, string password, CancellationToken ct = default)
    {
        var existing = await _users.GetByUsernameAsync(username, ct);
        if (existing != null) return null;
        var user = new User { Username = username, PasswordHash = BCrypt.Net.BCrypt.HashPassword(password) };
        await _users.AddAsync(user, ct);
        var token = GenerateJwt(user, out var expiresAt);
        await _tokenStore.UpsertUserTokenAsync(user.Id, token, expiresAt, ct);
        return token;
    }

    public async Task<string?> LoginAsync(string username, string password, CancellationToken ct = default)
    {
        var user = await _users.GetByUsernameAsync(username, ct);
        if (user == null) return null;
        if (!BCrypt.Net.BCrypt.Verify(password, user.PasswordHash)) return null;
        var token = GenerateJwt(user, out var expiresAt);
        await _tokenStore.UpsertUserTokenAsync(user.Id, token, expiresAt, ct);
        return token;
    }

    private string GenerateJwt(User user, out DateTimeOffset expiresAt)
    {
        var key = _config["Jwt:Key"] ?? throw new InvalidOperationException("Jwt:Key não configurado");
        if (Encoding.UTF8.GetByteCount(key) < 32)
        {
            throw new InvalidOperationException("Jwt:Key precisa ter pelo menos 256 bits (32 bytes).");
        }
        var issuer = _config["Jwt:Issuer"] ?? "RouterManager";
        var creds = new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)), SecurityAlgorithms.HmacSha256);
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.Username),
            new Claim(ClaimTypes.Role, user.Role)
        };
        expiresAt = DateTimeOffset.UtcNow.AddHours(4);
        var token = new JwtSecurityToken(issuer, null, claims, expires: expiresAt.UtcDateTime, signingCredentials: creds);
        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}