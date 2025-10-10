using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RouterManager.Infrastructure.Persistence;
using RouterManager.Shared.Dtos.Requests;
using RouterManager.Domain.Entities;
using System.Security.Claims;

namespace RouterManager.Api.Controllers;

[ApiController]
[Route("api/v1/routerprofiles")] // v1
[Authorize]
public class RouterProfilesController : ControllerBase
{
    private readonly RouterManagerDbContext _db;
    public RouterProfilesController(RouterManagerDbContext db) => _db = db;

    // POST: /api/v1/routerprofiles
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create([FromBody] CreateRouterProfileRequest req, CancellationToken ct)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var userId = GetCurrentUserId();
        if (userId == null) return Unauthorized();

        if (string.IsNullOrWhiteSpace(req.Ip)) return BadRequest("Ip requerido");
        if (string.IsNullOrWhiteSpace(req.SerialNumber)) return BadRequest("Serial requerido");
        if (string.IsNullOrWhiteSpace(req.Username)) return BadRequest("Username requerido");
        if (string.IsNullOrWhiteSpace(req.Password)) return BadRequest("Password requerida");

        var exists = await _db.RouterProfiles.AnyAsync(r => r.SerialNumber == req.SerialNumber && r.UserId == userId.Value, ct);
        if (exists) return Conflict("Um perfil com este número de série já existe para este usuário.");

        var entity = new RouterProfile
        {
            Ip = req.Ip.Trim(),
            Username = req.Username.Trim(),
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(req.Password),
            SerialNumber = req.SerialNumber.Trim(),
            Model = req.Model?.Trim() ?? string.Empty,
            CreatedAt = DateTime.UtcNow,
            UserId = userId.Value
        };
        _db.RouterProfiles.Add(entity);
        await _db.SaveChangesAsync(ct);

        var response = new
        {
            entity.Id,
            entity.Ip,
            entity.Username,
            entity.SerialNumber,
            entity.Model,
            entity.CreatedAt
        };

        return CreatedAtAction(nameof(GetById), new { id = entity.Id }, response);
    }

    // GET: /api/v1/routerprofiles
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> GetAllForCurrentUser(CancellationToken ct)
    {
        var userId = GetCurrentUserId();
        if (userId == null) return Unauthorized();

        var profiles = await _db.RouterProfiles
            .AsNoTracking()
            .Where(p => p.UserId == userId.Value)
            .OrderByDescending(p => p.CreatedAt)
            .Select(p => new
            {
                p.Id,
                p.Ip,
                p.Username,
                p.SerialNumber,
                p.Model,
                p.CreatedAt
            })
            .ToListAsync(ct);

        return Ok(profiles);
    }

    // GET: /api/v1/routerprofiles/{id}
    [HttpGet("{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(int id, CancellationToken ct)
    {
        var userId = GetCurrentUserId();
        if (userId == null) return Unauthorized();

        var entity = await _db.RouterProfiles.AsNoTracking()
            .FirstOrDefaultAsync(r => r.Id == id && r.UserId == userId.Value, ct);

        if (entity == null) return NotFound();

        return Ok(new { entity.Id, entity.Ip, entity.Username, entity.SerialNumber, entity.Model, entity.CreatedAt, entity.UserId });
    }

    // PUT: /api/v1/routerprofiles/{id}
    [HttpPut("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateRouterProfileRequest req, CancellationToken ct)
    {
        if (id != req.Id) return BadRequest("O ID na URL não corresponde ao ID no corpo da requisição.");

        var userId = GetCurrentUserId();
        if (userId == null) return Unauthorized();

        var entity = await _db.RouterProfiles.FirstOrDefaultAsync(r => r.Id == id && r.UserId == userId.Value, ct);
        if (entity == null) return NotFound();

        if (string.IsNullOrWhiteSpace(req.Ip)) return BadRequest("Ip requerido");
        if (string.IsNullOrWhiteSpace(req.SerialNumber)) return BadRequest("Serial requerido");
        if (string.IsNullOrWhiteSpace(req.Username)) return BadRequest("Username requerido");

        entity.Ip = req.Ip.Trim();
        entity.Username = req.Username.Trim();
        entity.SerialNumber = req.SerialNumber.Trim();
        entity.Model = req.Model?.Trim() ?? string.Empty;

        if (!string.IsNullOrWhiteSpace(req.Password))
        {
            entity.PasswordHash = BCrypt.Net.BCrypt.HashPassword(req.Password);
        }

        await _db.SaveChangesAsync(ct);
        return NoContent();
    }

    // DELETE: /api/v1/routerprofiles/{id}
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var userId = GetCurrentUserId();
        if (userId == null) return Unauthorized();

        var entity = await _db.RouterProfiles.FirstOrDefaultAsync(r => r.Id == id && r.UserId == userId.Value, ct);
        if (entity == null) return NotFound();

        _db.RouterProfiles.Remove(entity);
        await _db.SaveChangesAsync(ct);
        return NoContent();
    }

    private int? GetCurrentUserId()
    {
        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        return int.TryParse(userIdClaim, out var userId) ? userId : null;
    }
}
