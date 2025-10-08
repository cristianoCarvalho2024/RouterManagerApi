using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RouterManager.Application.Services;
using RouterManager.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace RouterManager.Api.Controllers;

[ApiController]
[Route("api/devices")]
[Route("api/v1/devices")] // versão 1
public class DevicesController : ControllerBase
{
    private readonly IAuthService _authService;
    private readonly RouterManagerDbContext _db;
    public DevicesController(IAuthService authService, RouterManagerDbContext db)
    {
        _authService = authService;
        _db = db;
    }

    public record DeviceRegistrationRequest(string DeviceId);

    [AllowAnonymous]
    [HttpPost("register")]
    public async Task<IActionResult> RegisterDevice([FromBody] DeviceRegistrationRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.DeviceId))
            return BadRequest("DeviceId é obrigatório.");

        // Primeiro tenta registrar; se já existir, faz login
        var token = await _authService.RegisterAsync(request.DeviceId, request.DeviceId, ct)
                    ?? await _authService.LoginAsync(request.DeviceId, request.DeviceId, ct);

        return Ok(new { token });
    }

    // Lista dispositivos (somente Admin)
    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var list = await _db.Devices
            .AsNoTracking()
            .Include(d => d.RouterModel)
                .ThenInclude(m => m.Provider)
            .OrderByDescending(d => d.LastSeen)
            .Select(d => new
            {
                d.Id,
                d.SerialNumber,
                d.FirmwareVersion,
                d.LastSeen,
                Model = d.RouterModel.Name,
                Provider = d.RouterModel.Provider.Name
            })
            .ToListAsync(ct);
        return Ok(list);
    }
}
