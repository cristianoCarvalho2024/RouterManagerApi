using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RouterManager.Infrastructure.Persistence;
using RouterManager.Domain.Entities;

namespace RouterManager.Api.Controllers;

[ApiController]
[Route("api/v1/update-orders")]
[Authorize(Policy = "AdminOnly")]
public class UpdateOrdersController : ControllerBase
{
    private readonly RouterManagerDbContext _db;
    public UpdateOrdersController(RouterManagerDbContext db) => _db = db;

    public record CreateOrderRequest(int ProviderId, string ModelIdentifier, string? FirmwareVersion, string RequestPayload);

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateOrderRequest req, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(req.RequestPayload)) return BadRequest("RequestPayload requerido");
        var entity = new UpdatePackage
        {
            ProviderId = req.ProviderId,
            ModelIdentifier = req.ModelIdentifier,
            FirmwareVersion = req.FirmwareVersion,
            RequestPayload = req.RequestPayload,
            CreatedAt = DateTime.UtcNow
        };
        _db.UpdatePackages.Add(entity);
        await _db.SaveChangesAsync(ct);
        return CreatedAtAction(nameof(GetById), new { id = entity.Id }, new { entity.Id });
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id, CancellationToken ct)
    {
        var e = await _db.UpdatePackages.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id, ct);
        if (e == null) return NotFound();
        return Ok(new { e.Id, e.ProviderId, e.ModelIdentifier, e.FirmwareVersion, e.RequestPayload, e.CreatedAt });
    }
}
