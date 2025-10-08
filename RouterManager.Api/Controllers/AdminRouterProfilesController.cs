using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RouterManager.Infrastructure.Persistence;

namespace RouterManager.Api.Controllers;

[ApiController]
[Route("api/admin/routerprofiles")]
[Authorize(Roles = "Admin")]
public class AdminRouterProfilesController : ControllerBase
{
    private readonly RouterManagerDbContext _db;
    public AdminRouterProfilesController(RouterManagerDbContext db) => _db = db;

    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var list = await _db.RouterProfiles
            .AsNoTracking()
            .OrderByDescending(r => r.CreatedAt)
            .Select(r => new { r.Id, r.Ip, r.Username, r.SerialNumber, r.Model, r.CreatedAt, r.UserId })
            .ToListAsync(ct);
        return Ok(list);
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        var entity = await _db.RouterProfiles.FirstOrDefaultAsync(r => r.Id == id, ct);
        if (entity == null) return NotFound();
        _db.RouterProfiles.Remove(entity);
        await _db.SaveChangesAsync(ct);
        return NoContent();
    }
}
