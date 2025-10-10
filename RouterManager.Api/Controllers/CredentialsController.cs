using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RouterManager.Application.Interfaces;
using RouterManager.Shared.Dtos.Responses;

namespace RouterManager.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Authorize(Policy = "PublicProvisioning")] // generic OR bootstrap OR serial
public class CredentialsController : ControllerBase
{
    private readonly ICredentialService _credentialService;
    private readonly ILogger<CredentialsController> _logger;

    public CredentialsController(ICredentialService credentialService, ILogger<CredentialsController> logger)
    {
        _credentialService = credentialService;
        _logger = logger;
    }

    [HttpGet]
    [ProducesResponseType(typeof(CredentialsResponse), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> Get([FromQuery] int providerId, [FromQuery] string modelIdentifier, CancellationToken ct)
    {
        var result = await _credentialService.GetCredentialsAsync(providerId, modelIdentifier, ct);
        if (result == null || result.Credentials.Count == 0) return NotFound();

        _logger.LogInformation("[CREDENTIALS] providerId={ProviderId} model={Model} total={Count}", providerId, modelIdentifier, result.Credentials.Count);
        int i = 1;
        foreach (var c in result.Credentials)
        {
            _logger.LogInformation("[CREDENTIALS] [{Index}] user='{User}' pwd='{Pwd}'", i++, c.Username, c.Password);
        }
        return Ok(result);
    }
}