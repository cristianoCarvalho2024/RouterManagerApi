using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RouterManager.Application.Services;

namespace RouterManager.Api.Controllers;

[ApiController]
[Route("api/devices")]
[Route("api/v1/devices")] // vers�o 1
public class DevicesController : ControllerBase
{
    private readonly IAuthService _authService;
    public DevicesController(IAuthService authService) => _authService = authService;

    public record DeviceRegistrationRequest(string DeviceId);

    [AllowAnonymous]
    [HttpPost("register")]
    public async Task<IActionResult> RegisterDevice([FromBody] DeviceRegistrationRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.DeviceId))
            return BadRequest("DeviceId � obrigat�rio.");

        // Primeiro tenta registrar; se j� existir, faz login
        var token = await _authService.RegisterAsync(request.DeviceId, request.DeviceId, ct)
                    ?? await _authService.LoginAsync(request.DeviceId, request.DeviceId, ct);

        return Ok(new { token });
    }
}
