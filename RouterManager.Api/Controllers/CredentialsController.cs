using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RouterManager.Application.Interfaces;
using RouterManager.Shared.Dtos.Responses;

namespace RouterManager.Api.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[AllowAnonymous] // protegido por API Key via middleware; não exige JWT
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

        // Log detalhado de todas as credenciais retornadas
        _logger.LogInformation("[CREDENTIALS] providerId={ProviderId} model={Model} total={Count}", providerId, modelIdentifier, result.Credentials.Count);
        int i = 1;
        foreach (var c in result.Credentials)
        {
            _logger.LogInformation("[CREDENTIALS] [{Index}] user='{User}' pwd='{Pwd}'", i++, c.Username, c.Password);
        }
        return Ok(result);
    }

    // Atalho para evitar chamada extra ao /providers no App
    [HttpGet("by-name")]
    [ProducesResponseType(typeof(CredentialsResponse), 200)]
    [ProducesResponseType(404)]
    public async Task<IActionResult> GetByProviderName([FromQuery] string providerName, [FromQuery] string modelIdentifier, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(providerName)) return NotFound();
        // Reutiliza o endpoint principal após resolver o providerId no app, mantendo o log centralizado aqui.
        return await Get(await ResolveProviderIdAsync(providerName, ct), modelIdentifier, ct);
    }

    private async Task<int> ResolveProviderIdAsync(string providerName, CancellationToken ct)
    {
        // Fallback simples por enquanto: o app já remapeia pelo /providers.
        // Mantemos 0 para provocar 404 se não resolvido corretamente pelo app.
        return 0;
    }
}