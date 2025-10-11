using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RouterManager.Application.Services;
using RouterManager.Api.Models;
using RouterManager.Application.Interfaces;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;
using System.Text;

namespace RouterManager.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _auth;
    private readonly ILogger<AuthController> _logger;
    private readonly IWebHostEnvironment _env;
    private readonly IConfiguration _config;
    private readonly ITokenStore _tokenStore;

    public AuthController(IAuthService auth, ILogger<AuthController> logger, IWebHostEnvironment env, IConfiguration config, ITokenStore tokenStore)
    {
        _auth = auth;
        _logger = logger;
        _env = env;
        _config = config;
        _tokenStore = tokenStore;
    }

    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request, CancellationToken ct)
    {
        if (!_env.IsDevelopment())
        {
            return NotFound(); // desabilitado fora de Development
        }

        var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
        _logger.LogInformation("Auth REGISTER attempt: user={User} passLen={Len} ip={Ip}", request?.Username, request?.Password?.Length ?? 0, ip);

        if (!ModelState.IsValid) return ValidationProblem(ModelState);
        var token = await _auth.RegisterAsync(request.Username, request.Password, ct);
        if (token == null) return BadRequest("User exists");
        return Created(string.Empty, new { token });
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] LoginRequest request, CancellationToken ct)
    {
        var ip = HttpContext.Connection.RemoteIpAddress?.ToString();
        _logger.LogInformation("Auth LOGIN attempt: user={User} passLen={Len} ip={Ip}", request?.Username, request?.Password?.Length ?? 0, ip);

        if (!ModelState.IsValid) return ValidationProblem(ModelState);
        var token = await _auth.LoginAsync(request.Username, request.Password, ct);
        if (token == null)
        {
            _logger.LogWarning("Auth LOGIN failed: user={User} ip={Ip}", request?.Username, ip);
            return Unauthorized();
        }
        _logger.LogInformation("Auth LOGIN success: user={User} ip={Ip}", request?.Username, ip);
        return Ok(new { token });
    }

    [HttpPost("refresh")]
    [Authorize]
    public async Task<IActionResult> Refresh(CancellationToken ct)
    {
        var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(userIdStr, out var userId) || userId <= 0)
        {
            return Unauthorized();
        }

        var role = User.FindFirstValue(ClaimTypes.Role) ?? string.Empty;
        var name = User.FindFirstValue(ClaimTypes.Name) ?? string.Empty;

        var key = _config["Jwt:Key"] ?? throw new InvalidOperationException("Jwt:Key não configurado");
        if (Encoding.UTF8.GetByteCount(key) < 32)
        {
            throw new InvalidOperationException("Jwt:Key precisa ter pelo menos 256 bits (32 bytes).");
        }
        var issuer = _config["Jwt:Issuer"] ?? "RouterManager";
        var creds = new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)), SecurityAlgorithms.HmacSha256);

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString())
        };
        if (!string.IsNullOrWhiteSpace(name)) claims.Add(new Claim(ClaimTypes.Name, name));
        if (!string.IsNullOrWhiteSpace(role)) claims.Add(new Claim(ClaimTypes.Role, role));

        var expiresAt = DateTimeOffset.UtcNow.AddHours(4);
        var jwt = new JwtSecurityToken(issuer, null, claims, expires: expiresAt.UtcDateTime, signingCredentials: creds);
        var token = new JwtSecurityTokenHandler().WriteToken(jwt);

        await _tokenStore.UpsertUserTokenAsync(userId, token, expiresAt, ct);
        return Ok(new { token });
    }
}