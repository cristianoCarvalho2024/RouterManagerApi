using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RouterManager.Application.Interfaces;
using RouterManager.Shared.Dtos.Requests;

namespace RouterManager.Api.Controllers;

[ApiController]
[Route("api/v1/telemetry")]
[Route("api/telemetry")] // alias sem versão para compatibilidade
[Authorize]
public class TelemetryController : ControllerBase
{
    private readonly ITelemetryService _telemetryService;
    public TelemetryController(ITelemetryService telemetryService) => _telemetryService = telemetryService;

    [HttpPost("report")]
    [ProducesResponseType(202)]
    public async Task<IActionResult> Report([FromBody] ReportStatusRequest request, CancellationToken ct)
    {
        await _telemetryService.ReportAsync(request, ct);
        return Accepted();
    }
}