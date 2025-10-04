using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RouterManager.Application.Services;

namespace RouterManager.Api.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _auth;
    public AuthController(IAuthService auth) => _auth = auth;

    public record AuthRequest(string Username, string Password);

    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<IActionResult> Register([FromBody] AuthRequest request, CancellationToken ct)
    {
        var token = await _auth.RegisterAsync(request.Username, request.Password, ct);
        if (token == null) return BadRequest("User exists");
        return Created(string.Empty, new { token });
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<IActionResult> Login([FromBody] AuthRequest request, CancellationToken ct)
    {
        var token = await _auth.LoginAsync(request.Username, request.Password, ct);
        if (token == null) return Unauthorized();
        return Ok(new { token });
    }
}