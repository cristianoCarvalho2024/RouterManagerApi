using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RouterManager.Application.Interfaces;

namespace RouterManager.Api.Controllers;

[ApiController]
[Route("api/providers")]
[Authorize]
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

    [HttpGet("{id}/credentials")]
    public async Task<IActionResult> GetCredentials(int id, [FromQuery] string model, CancellationToken ct)
    {
        var creds = await _service.GetCredentialsAsync(id, model, ct);
        return Ok(creds.Select(c => new { c.Username, c.Password }));
    }
}