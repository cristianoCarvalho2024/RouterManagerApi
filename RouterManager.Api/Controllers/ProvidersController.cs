using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RouterManager.Application.Interfaces;

namespace RouterManager.Api.Controllers;

[ApiController]
[Route("api/providers")]
[Route("api/v1/providers")] // alias v1
[Authorize(Policy = "PublicProviders")] // generic, bootstrap, serial, provider, admin
public class ProvidersController : ControllerBase
{
    private readonly IProvidersService _service;
    public ProvidersController(IProvidersService service) => _service = service;

    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken ct)
    {
        var list = await _service.GetAllAsync(ct);
        return Ok(list.Select(p => new { p.Id, p.Name }));
    }

    [HttpGet("by-name")]
    public async Task<IActionResult> GetByName([FromQuery] string name, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(name)) return BadRequest("name requerido");
        var list = await _service.GetAllAsync(ct);
        var match = list.FirstOrDefault(p => string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase));
        if (match == null) return NotFound();
        return Ok(new { match.Id, match.Name });
    }
}