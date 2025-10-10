using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RouterManager.Infrastructure.Persistence;
using RouterManager.Domain.Entities;
using System.Text.Json;
using RouterManager.Api.Models.UpdateOrders;

namespace RouterManager.Api.Controllers;

[ApiController]
[Route("api/v1/update-orders")] // Rota correta
[Authorize]
public class UpdateOrdersController : ControllerBase
{
    private readonly RouterManagerDbContext _db;

    public UpdateOrdersController(RouterManagerDbContext db) => _db = db;

    // GET: /api/v1/update-orders
    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var orders = await _db.UpdatePackages
            .AsNoTracking()
            .OrderByDescending(o => o.CreatedAt)
            .Select(o => new { o.Id, o.Name, o.CreatedAt })
            .ToListAsync(ct);
        return Ok(orders);
    }

    // GET: /api/v1/update-orders/{id}
    [HttpGet("{id:int}")]
    [ProducesResponseType(typeof(UpdateOrderDetailDto), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetById(int id, CancellationToken ct)
    {
        var o = await _db.UpdatePackages
            .AsNoTracking()
            .FirstOrDefaultAsync(x => x.Id == id, ct);

        if (o == null) return NotFound();

        var dto = new UpdateOrderDetailDto(
            o.Id,
            o.Name,
            o.ProviderId,
            o.ModelIdentifier,
            o.FirmwareVersion,
            o.SerialNumber,
            o.RequestPayload,
            o.CreatedAt
        );
        return Ok(dto);
    }

    // POST: /api/v1/update-orders
    [HttpPost]
    [ProducesResponseType(typeof(object), 201)]
    [ProducesResponseType(400)]
    public async Task<IActionResult> Create([FromBody] CreateUpdateOrderRequest req, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(req.Name))
            return BadRequest("O nome da ordem é obrigatório.");
        if (req.ProviderId <= 0)
            return BadRequest("ProviderId inválido.");
        if (string.IsNullOrWhiteSpace(req.ModelIdentifier))
            return BadRequest("ModelIdentifier é obrigatório.");

        var entity = new UpdatePackage
        {
            Name = req.Name.Trim(),
            ProviderId = req.ProviderId,
            ModelIdentifier = req.ModelIdentifier.Trim(),
            FirmwareVersion = string.IsNullOrWhiteSpace(req.FirmwareVersion) ? null : req.FirmwareVersion.Trim(),
            SerialNumber = string.IsNullOrWhiteSpace(req.SerialNumber) ? null : req.SerialNumber.Trim(),
            RequestPayload = JsonSerializer.Serialize(req.Actions),
            CreatedAt = DateTime.UtcNow
        };

        _db.UpdatePackages.Add(entity);
        await _db.SaveChangesAsync(ct);

        return CreatedAtAction(nameof(GetById), new { id = entity.Id }, new { entity.Id });
    }

    // PUT: /api/v1/update-orders/{id}
    [HttpPut("{id:int}")]
    [ProducesResponseType(204)]
    [ProducesResponseType(400)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateUpdateOrderRequest req, CancellationToken ct)
    {
        if (id != req.Id)
            return BadRequest("O ID da rota não corresponde ao ID do corpo da requisição.");
        if (string.IsNullOrWhiteSpace(req.Name))
            return BadRequest("O nome da ordem é obrigatório.");
        if (req.ProviderId <= 0)
            return BadRequest("ProviderId inválido.");
        if (string.IsNullOrWhiteSpace(req.ModelIdentifier))
            return BadRequest("ModelIdentifier é obrigatório.");

        var entity = await _db.UpdatePackages.FirstOrDefaultAsync(o => o.Id == id, ct);
        if (entity == null)
            return NotFound();

        entity.Name = req.Name.Trim();
        entity.ProviderId = req.ProviderId;
        entity.ModelIdentifier = req.ModelIdentifier.Trim();
        entity.FirmwareVersion = string.IsNullOrWhiteSpace(req.FirmwareVersion) ? null : req.FirmwareVersion.Trim();
        entity.SerialNumber = string.IsNullOrWhiteSpace(req.SerialNumber) ? null : req.SerialNumber.Trim();
        entity.RequestPayload = JsonSerializer.Serialize(req.Actions);

        await _db.SaveChangesAsync(ct);
        return NoContent();
    }

    // DELETE: /api/v1/update-orders/{id}
    [HttpDelete("{id:int}")]
    [ProducesResponseType(204)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var entity = await _db.UpdatePackages.FirstOrDefaultAsync(o => o.Id == id, ct);
        if (entity == null)
            return NotFound();

        _db.UpdatePackages.Remove(entity);
        await _db.SaveChangesAsync(ct);

        return NoContent();
    }
}
