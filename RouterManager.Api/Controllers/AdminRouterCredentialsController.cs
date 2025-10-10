using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RouterManager.Infrastructure.Persistence;

namespace RouterManager.Api.Controllers;

[ApiController]
[Route("api/admin/routercredentials")]
[Authorize(Roles = "Admin")]
public class AdminRouterCredentialsController : ControllerBase
{
    private readonly RouterManagerDbContext _db;
    public AdminRouterCredentialsController(RouterManagerDbContext db) => _db = db;

    public sealed class CreateCredentialRequest
    {
        public int RouterModelId { get; set; }
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public int SortOrder { get; set; } = 0;
    }

    public sealed class UpdateCredentialRequest
    {
        public string? Username { get; set; }
        public string? Password { get; set; }
        public int? RouterModelId { get; set; }
        public int? SortOrder { get; set; }
    }

    public sealed class ReorderRequest
    {
        public int RouterModelId { get; set; }
        public List<Item> Items { get; set; } = new();
        public sealed class Item { public int Id { get; set; } public int SortOrder { get; set; } }
    }

    // GET: /api/admin/routercredentials
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll([FromQuery] int? routerModelId, CancellationToken ct)
    {
        var query = _db.RouterCredentials.AsNoTracking().Include(c => c.RouterModel).ThenInclude(m => m.Provider).AsQueryable();
        if (routerModelId.HasValue) query = query.Where(c => c.RouterModelId == routerModelId.Value);

        // Materializa primeiro, depois descriptografa em memória com fallback
        var raw = await query
            .OrderBy(c => c.RouterModel.Provider.Name)
            .ThenBy(c => c.RouterModel.Name)
            .ThenBy(c => c.SortOrder)
            .ThenBy(c => c.Id)
            .Select(c => new
            {
                c.Id,
                c.Username,
                c.PasswordEncrypted,
                c.RouterModelId,
                Model = c.RouterModel.Name,
                Provider = c.RouterModel.Provider.Name,
                c.SortOrder
            })
            .ToListAsync(ct);

        string SafeUnprotect(string enc)
        {
            try { return _db.Unprotect(enc); } catch { return string.Empty; }
        }

        var list = raw.Select(c => new
        {
            c.Id,
            c.Username,
            Password = SafeUnprotect(c.PasswordEncrypted),
            c.RouterModelId,
            c.Model,
            c.Provider,
            c.SortOrder
        }).ToList();

        return Ok(list);
    }

    // GET: /api/admin/routercredentials/{id}
    [HttpGet("{id:int}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(int id, CancellationToken ct)
    {
        var e = await _db.RouterCredentials.AsNoTracking().Include(c => c.RouterModel).ThenInclude(m => m.Provider).FirstOrDefaultAsync(c => c.Id == id, ct);
        if (e == null) return NotFound();
        string password;
        try { password = _db.Unprotect(e.PasswordEncrypted); } catch { password = string.Empty; }
        return Ok(new { e.Id, e.Username, Password = password, e.RouterModelId, Model = e.RouterModel.Name, Provider = e.RouterModel.Provider.Name, e.SortOrder });
    }

    // POST: /api/admin/routercredentials
    [HttpPost]
    [ProducesResponseType(typeof(object), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Create([FromBody] CreateCredentialRequest req, CancellationToken ct)
    {
        if (req.RouterModelId <= 0) return BadRequest("RouterModelId requerido");
        if (string.IsNullOrWhiteSpace(req.Username) || string.IsNullOrWhiteSpace(req.Password)) return BadRequest("Username e Password requeridos");
        var model = await _db.RouterModels.FirstOrDefaultAsync(m => m.Id == req.RouterModelId, ct);
        if (model == null) return BadRequest("Modelo inválido");
        var entity = new RouterManager.Domain.Entities.RouterCredential
        {
            RouterModelId = req.RouterModelId,
            Username = req.Username.Trim(),
            PasswordEncrypted = _db.Protect(req.Password),
            SortOrder = req.SortOrder
        };
        _db.RouterCredentials.Add(entity);
        await _db.SaveChangesAsync(ct);
        return CreatedAtAction(nameof(GetById), new { id = entity.Id }, new { entity.Id });
    }

    // PUT: /api/admin/routercredentials/{id}
    [HttpPut("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateCredentialRequest req, CancellationToken ct)
    {
        var entity = await _db.RouterCredentials.FirstOrDefaultAsync(c => c.Id == id, ct);
        if (entity == null) return NotFound();
        if (!string.IsNullOrWhiteSpace(req.Username)) entity.Username = req.Username.Trim();
        if (!string.IsNullOrWhiteSpace(req.Password)) entity.PasswordEncrypted = _db.Protect(req.Password);
        if (req.RouterModelId.GetValueOrDefault() > 0) entity.RouterModelId = req.RouterModelId!.Value;
        if (req.SortOrder.HasValue) entity.SortOrder = req.SortOrder.Value;
        await _db.SaveChangesAsync(ct);
        return NoContent();
    }

    // PUT: /api/admin/routercredentials/reorder
    [HttpPut("reorder")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Reorder([FromBody] ReorderRequest req, CancellationToken ct)
    {
        if (req.RouterModelId <= 0) return BadRequest("RouterModelId requerido");
        var ids = req.Items.Select(i => i.Id).ToHashSet();
        var creds = await _db.RouterCredentials.Where(c => c.RouterModelId == req.RouterModelId && ids.Contains(c.Id)).ToListAsync(ct);
        foreach (var c in creds)
        {
            var item = req.Items.FirstOrDefault(i => i.Id == c.Id);
            if (item != null) c.SortOrder = item.SortOrder;
        }
        await _db.SaveChangesAsync(ct);
        return NoContent();
    }

    // DELETE: /api/admin/routercredentials/{id}
    [HttpDelete("{id:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var entity = await _db.RouterCredentials.FirstOrDefaultAsync(c => c.Id == id, ct);
        if (entity == null) return NotFound();
        _db.RouterCredentials.Remove(entity);
        await _db.SaveChangesAsync(ct);
        return NoContent();
    }
}
