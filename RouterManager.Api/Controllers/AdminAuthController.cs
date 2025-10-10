using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RouterManager.Application.Interfaces;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;
using System.Text;

namespace RouterManager.Api.Controllers;

[ApiController]
[Route("api/admin/auth")]
[Authorize(Roles = "Admin")]
public class AdminAuthController : ControllerBase
{
    private readonly IConfiguration _config;
    private readonly ITokenStore _tokenStore;
    public AdminAuthController(IConfiguration config, ITokenStore tokenStore)
    {
        _config = config;
        _tokenStore = tokenStore;
    }

    public sealed class IssueProviderTokenRequest
    {
        public int ProviderId { get; set; }
        public int DaysToExpire { get; set; } = 30;
    }

    [HttpPost("provider-token")]
    public async Task<IActionResult> IssueProviderToken([FromBody] IssueProviderTokenRequest req, CancellationToken ct)
    {
        if (req.ProviderId <= 0) return BadRequest("ProviderId inválido");
        var expires = DateTimeOffset.UtcNow.AddDays(Math.Clamp(req.DaysToExpire, 1, 365));
        var token = GenerateToken(new[] { new Claim("providerId", req.ProviderId.ToString()), new Claim(ClaimTypes.Role, "Provider") }, expires);
        await _tokenStore.UpsertProviderTokenAsync(req.ProviderId, token, expires, ct);
        return Ok(new { token, expiresAt = expires });
    }

    private string GenerateToken(IEnumerable<Claim> claims, DateTimeOffset expiresAt)
    {
        var key = _config["Jwt:Key"] ?? throw new InvalidOperationException("Jwt:Key não configurado.");
        var issuer = _config["Jwt:Issuer"] ?? "RouterManager";
        var creds = new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)), SecurityAlgorithms.HmacSha256);
        var jwt = new JwtSecurityToken(issuer, null, claims, expires: expiresAt.UtcDateTime, signingCredentials: creds);
        return new JwtSecurityTokenHandler().WriteToken(jwt);
    }
}
