using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using RouterManager.Infrastructure.Persistence;

namespace RouterManager.Api.Security;

public class DbTokenAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    public const string SchemeName = "DbToken";
    private readonly RouterManagerDbContext _db;

    private record JwtTokenRow(string Kind, string? Serial, int? ProviderId, int? UserId, string Token);

    public DbTokenAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        ISystemClock clock,
        RouterManagerDbContext db) : base(options, logger, encoder, clock)
    {
        _db = db;
    }

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue("Authorization", out var hdr))
            return AuthenticateResult.NoResult();
        var value = hdr.ToString();
        if (string.IsNullOrWhiteSpace(value)) return AuthenticateResult.NoResult();
        var token = value.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)
            ? value.Substring("Bearer ".Length).Trim()
            : value.Trim();
        if (string.IsNullOrWhiteSpace(token)) return AuthenticateResult.NoResult();

        try
        {
            var row = await _db.Database
                .SqlQuery<JwtTokenRow>($"SELECT TOP 1 Kind, Serial, ProviderId, UserId, Token FROM dbo.JwtTokens WHERE Token = {token}")
                .FirstOrDefaultAsync();
            if (row == null) return AuthenticateResult.Fail("Token not found");

            var claims = new List<Claim>();
            switch (row.Kind)
            {
                case "device":
                    if (string.Equals(row.Serial, "GENERIC_APP", StringComparison.OrdinalIgnoreCase))
                    {
                        claims.Add(new Claim("type", "generic"));
                    }
                    else if (!string.IsNullOrWhiteSpace(row.Serial))
                    {
                        claims.Add(new Claim("serial", row.Serial));
                    }
                    break;
                case "provider":
                    if (row.ProviderId.HasValue)
                    {
                        claims.Add(new Claim("providerId", row.ProviderId.Value.ToString()));
                        claims.Add(new Claim(ClaimTypes.Role, "Provider"));
                    }
                    break;
                case "user":
                    if (row.UserId.HasValue)
                    {
                        claims.Add(new Claim(ClaimTypes.NameIdentifier, row.UserId.Value.ToString()));
                        claims.Add(new Claim(ClaimTypes.Role, "Admin")); // super admin fixo
                    }
                    break;
            }
            if (claims.Count == 0)
                return AuthenticateResult.Fail("Unsupported token kind");

            var identity = new ClaimsIdentity(claims, SchemeName);
            var principal = new ClaimsPrincipal(identity);
            var ticket = new AuthenticationTicket(principal, SchemeName);
            return AuthenticateResult.Success(ticket);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "DbToken authentication failed");
            return AuthenticateResult.Fail("DbToken auth error");
        }
    }
}
