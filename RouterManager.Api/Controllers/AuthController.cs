using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RouterManager.Application.Services;
using RouterManager.Api.Models;

namespace RouterManager.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _auth;
    private readonly ILogger<AuthController> _logger;
    public AuthController(IAuthService auth, ILogger<AuthController> logger)
    {
        _auth = auth;
        _logger = logger;
    }

    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request, CancellationToken ct)
    {
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
}