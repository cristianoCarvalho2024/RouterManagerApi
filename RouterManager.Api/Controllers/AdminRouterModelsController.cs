using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RouterManager.Infrastructure.Persistence;

namespace RouterManager.Api.Controllers;

[ApiController]
[Route("api/admin/routermodels")]
[Authorize(Roles = "Admin")]
public class AdminRouterModelsController : ControllerBase
{
    private readonly RouterManagerDbContext _db;
    public AdminRouterModelsController(RouterManagerDbContext db) => _db = db;

    public sealed class ModelRequest
    {
        public string Name { get; set; } = string.Empty;
        public string EnumIdentifier { get; set; } = "Unknown";
        public int ProviderId { get; set; }
    }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] int? providerId, CancellationToken ct)
    {
        var query = _db.RouterModels.AsNoTracking().Include(m => m.Provider).AsQueryable();
        if (providerId.HasValue) query = query.Where(m => m.ProviderId == providerId.Value);
        var list = await query
            .OrderBy(m => m.Name)
            .Select(m => new { m.Id, m.Name, m.EnumIdentifier, m.ProviderId, Provider = m.Provider.Name })
            .ToListAsync(ct);
        return Ok(list);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] ModelRequest req, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(req.Name)) return BadRequest("Name requerido");
        var provider = await _db.Providers.FirstOrDefaultAsync(p => p.Id == req.ProviderId, ct);
        if (provider == null) return BadRequest("Provider inválido");
        var entity = new RouterManager.Domain.Entities.RouterModel
        {
            Name = req.Name.Trim(),
            EnumIdentifier = Enum.TryParse<RouterManager.Domain.Entities.RouterModelIdentifier>(req.EnumIdentifier, out var id) ? id : RouterManager.Domain.Entities.RouterModelIdentifier.Unknown,
            ProviderId = req.ProviderId
        };
        _db.RouterModels.Add(entity);
        await _db.SaveChangesAsync(ct);
        return CreatedAtAction(nameof(GetAll), new { id = entity.Id }, new { entity.Id });
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] ModelRequest req, CancellationToken ct)
    {
        var entity = await _db.RouterModels.FirstOrDefaultAsync(m => m.Id == id, ct);
        if (entity == null) return NotFound();
        if (!string.IsNullOrWhiteSpace(req.Name)) entity.Name = req.Name.Trim();
        if (!string.IsNullOrWhiteSpace(req.EnumIdentifier) && Enum.TryParse<RouterManager.Domain.Entities.RouterModelIdentifier>(req.EnumIdentifier, out var parsed))
            entity.EnumIdentifier = parsed;
        if (req.ProviderId > 0) entity.ProviderId = req.ProviderId;
        await _db.SaveChangesAsync(ct);
        return NoContent();
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var entity = await _db.RouterModels.FirstOrDefaultAsync(m => m.Id == id, ct);
        if (entity == null) return NotFound();
        var inUse = await _db.Devices.AnyAsync(d => d.RouterModelId == id, ct);
        if (inUse) return Conflict("Modelo em uso por devices.");
        _db.RouterModels.Remove(entity);
        await _db.SaveChangesAsync(ct);
        return NoContent();
    }
}
