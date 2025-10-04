using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RouterManager.Application.Interfaces;
using RouterManager.Shared.Dtos.Requests;
using RouterManager.Shared.Dtos.Responses;

namespace RouterManager.Api.Controllers;

[ApiController]
[Route("api/v1/updates")]
[Authorize]
public class UpdatesController : ControllerBase
{
    private readonly IUpdateService _updateService;
    public UpdatesController(IUpdateService updateService) => _updateService = updateService;

    [HttpPost("check")]
    [ProducesResponseType(typeof(UpdatePackageResponse), 200)]
    [ProducesResponseType(204)]
    public async Task<IActionResult> Check([FromBody] CheckForUpdateRequest request, CancellationToken ct)
    {
        var result = await _updateService.CheckAsync(request, ct);
        if (result == null) return NoContent();
        return Ok(result);
    }
}