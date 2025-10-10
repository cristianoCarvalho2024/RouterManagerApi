using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RouterManager.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using RouterManager.Application.Interfaces;

namespace RouterManager.Api.Controllers;

[ApiController]
[Route("api/devices")]
[Route("api/v1/devices")] // versão 1
public class DevicesController : ControllerBase
{
    private readonly RouterManagerDbContext _db;
    private readonly IConfiguration _config;
    private readonly ITokenStore _tokenStore;
    public DevicesController(RouterManagerDbContext db, IConfiguration config, ITokenStore tokenStore)
    {
        _db = db;
        _config = config;
        _tokenStore = tokenStore;
    }

    public record DeviceBootstrapRequest(string DeviceId);
    public record DeviceRegistrationRequest(string Serial);

    [AllowAnonymous]
    [HttpPost("bootstrap")] // emite um token temporário com claim type=bootstrap
    public IActionResult Bootstrap([FromBody] DeviceBootstrapRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.DeviceId)) return BadRequest("deviceId obrigatório");
        var token = GenerateToken(new[] { new Claim("type", "bootstrap"), new Claim("deviceId", request.DeviceId) }, TimeSpan.FromHours(4));
        return Ok(new { token });
    }

    [Authorize(Policy = "CanRegisterDevice")] // generic OR bootstrap
    [HttpPost("register")] // troca o token genérico/bootstrap por um token de dispositivo com claim serial
    public async Task<IActionResult> RegisterDevice([FromBody] DeviceRegistrationRequest request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(request.Serial)) return BadRequest("serial obrigatório");

        // 1) Reutiliza token já existente no banco, se houver
        var existing = await _tokenStore.GetDeviceTokenAsync(request.Serial, ct);
        if (existing.HasValue)
        {
            return Ok(new { token = existing.Value.Token });
        }

        // 2) Gera novo JWT por serial
        var expires = TimeSpan.FromDays(30);
        var token = GenerateToken(new[] { new Claim("serial", request.Serial) }, expires);
        await _tokenStore.UpsertDeviceTokenAsync(request.Serial, token, DateTimeOffset.UtcNow.Add(expires), ct);

        // 3) Persiste/atualiza a entidade Device para visibilidade no Admin
        var device = await _db.Devices.FirstOrDefaultAsync(d => d.SerialNumber == request.Serial, ct);
        if (device == null)
        {
            device = new Domain.Entities.Device
            {
                SerialNumber = request.Serial,
                LastSeen = DateTime.UtcNow
            };
            _db.Devices.Add(device);
            await _db.SaveChangesAsync(ct);
        }
        else
        {
            device.LastSeen = DateTime.UtcNow;
            await _db.SaveChangesAsync(ct);
        }

        return Ok(new { token });
    }

    private string GenerateToken(IEnumerable<Claim> claims, TimeSpan ttl)
    {
        var key = _config["Jwt:Key"] ?? throw new InvalidOperationException("Jwt:Key não configurado.");
        var issuer = _config["Jwt:Issuer"] ?? "RouterManager";
        var creds = new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)), SecurityAlgorithms.HmacSha256);
        var jwt = new JwtSecurityToken(issuer, null, claims, expires: DateTime.UtcNow.Add(ttl), signingCredentials: creds);
        return new JwtSecurityTokenHandler().WriteToken(jwt);
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
