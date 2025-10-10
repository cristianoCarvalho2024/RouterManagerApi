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

    public sealed class CreateRouterModelRequest
    {
        public string Name { get; set; } = string.Empty;
        public string EnumIdentifier { get; set; } = "Unknown";
        public int ProviderId { get; set; }
    }

    public sealed class UpdateRouterModelRequest
    {
        public string? Name { get; set; }
        public string? EnumIdentifier { get; set; }
        public int? ProviderId { get; set; }
    }

    // GET: /api/admin/routermodels
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll([FromQuery] int? providerId, CancellationToken ct)
    {
        var query = _db.RouterModels.AsNoTracking().Include(m => m.Provider).AsQueryable();
        if (providerId.HasValue) query = query.Where(m => m.ProviderId == providerId.Value);
        var list = await query
            .OrderBy(m => m.Name)
            .Select(m => new
            {
                m.Id,
                m.Name,
                Identifier = m.EnumIdentifier == RouterManager.Domain.Entities.RouterModelIdentifier.Unknown ? m.Name : m.EnumIdentifier.ToString(),
                m.ProviderId,
                ProviderName = m.Provider.Name
            })
            .ToListAsync(ct);
        return Ok(list);
    }

    // GET: /api/admin/routermodels/{id}
    [HttpGet("{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(int id, CancellationToken ct)
    {
        var entity = await _db.RouterModels.AsNoTracking().Include(m => m.Provider).FirstOrDefaultAsync(m => m.Id == id, ct);
        if (entity == null) return NotFound();
        return Ok(new
        {
            entity.Id,
            entity.Name,
            Identifier = entity.EnumIdentifier == RouterManager.Domain.Entities.RouterModelIdentifier.Unknown ? entity.Name : entity.EnumIdentifier.ToString(),
            entity.ProviderId,
            ProviderName = entity.Provider.Name
        });
    }

    // POST: /api/admin/routermodels
    [HttpPost]
    [ProducesResponseType(typeof(object), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreateRouterModelRequest req, CancellationToken ct)
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
        return CreatedAtAction(nameof(GetById), new { id = entity.Id }, new { entity.Id });
    }

    // PUT: /api/admin/routermodels/{id}
    [HttpPut("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateRouterModelRequest req, CancellationToken ct)
    {
        var entity = await _db.RouterModels.FirstOrDefaultAsync(m => m.Id == id, ct);
        if (entity == null) return NotFound();
        if (!string.IsNullOrWhiteSpace(req.Name)) entity.Name = req.Name.Trim();
        if (!string.IsNullOrWhiteSpace(req.EnumIdentifier) && Enum.TryParse<RouterManager.Domain.Entities.RouterModelIdentifier>(req.EnumIdentifier, out var parsed))
            entity.EnumIdentifier = parsed;
        if (req.ProviderId.GetValueOrDefault() > 0) entity.ProviderId = req.ProviderId!.Value;
        await _db.SaveChangesAsync(ct);
        return NoContent();
    }

    // DELETE: /api/admin/routermodels/{id}
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var entity = await _db.RouterModels.FirstOrDefaultAsync(m => m.Id == id, ct);
        if (entity == null) return NotFound();
        var inUse = await _db.Devices.AnyAsync(d => d.RouterModelId == id, ct);
        if (inUse) return Conflict("Modelo em uso por devices. Considere remover vínculos antes de apagar.");
        _db.RouterModels.Remove(entity);
        await _db.SaveChangesAsync(ct);
        return NoContent();
    }
}
