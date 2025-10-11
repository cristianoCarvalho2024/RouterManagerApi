using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RouterManager.Infrastructure.Persistence;

namespace RouterManager.Api.Controllers;

[ApiController]
[Route("api/public")] // rotas públicas controladas
public class PublicController : ControllerBase
{
    private readonly RouterManagerDbContext _db;
    public PublicController(RouterManagerDbContext db) => _db = db;

    public sealed class TokenResponse { public string Token { get; set; } = string.Empty; }

    [HttpGet("generic-token")]
    [AllowAnonymous]
    [ResponseCache(Duration = 60)]
    public async Task<IActionResult> GetGenericToken(CancellationToken ct)
    {
        // Lê token genérico persistido no seeder (Serial='GENERIC_APP', Kind='device').
        // Ao projetar para um tipo primitivo, EF espera a coluna com alias 'Value'.
        var token = await _db.Database
            .SqlQuery<string>($"SELECT TOP 1 Token AS [Value] FROM dbo.JwtTokens WHERE Kind='device' AND Serial='GENERIC_APP' ORDER BY Id DESC")
            .FirstOrDefaultAsync(ct);
        if (string.IsNullOrWhiteSpace(token)) return NotFound();
        return Ok(new TokenResponse { Token = token });
    }
}
