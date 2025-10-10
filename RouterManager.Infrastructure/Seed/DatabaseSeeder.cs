using RouterManager.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using RouterManager.Domain.Entities;
using BCrypt.Net;
using Microsoft.Extensions.Configuration;
using RouterManager.Application.Interfaces;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.Security.Cryptography;
using Microsoft.Data.SqlClient;

namespace RouterManager.Infrastructure.Seed;

public interface IDatabaseSeeder
{
    Task SeedAsync();
}

public class DatabaseSeeder : IDatabaseSeeder
{
    private readonly RouterManagerDbContext _ctx;
    private readonly IConfiguration _config;
    private readonly ITokenStore _tokenStore;
    public DatabaseSeeder(RouterManagerDbContext ctx, IConfiguration config, ITokenStore tokenStore)
    {
        _ctx = ctx;
        _config = config;
        _tokenStore = tokenStore;
    }

    public async Task SeedAsync()
    {
        if (_ctx.Database.IsRelational())
        {
            await _ctx.Database.MigrateAsync();
            await EnsureSchemaCompatAsync();
        }

        if (!await _ctx.Providers.AnyAsync())
        {
            var provider = new Provider { Name = "Default ISP" };
            _ctx.Providers.Add(provider);
            await _ctx.SaveChangesAsync();

            var model = new RouterModel { Name = "AX1000", EnumIdentifier = RouterModelIdentifier.ModelAX1000, ProviderId = provider.Id };
            _ctx.RouterModels.Add(model);
            await _ctx.SaveChangesAsync();

            var cred1 = new RouterCredential { Username = "admin", PasswordEncrypted = _ctx.Protect("admin123"), RouterModelId = model.Id };
            _ctx.RouterCredentials.AddRange(cred1);
            await _ctx.SaveChangesAsync();
        }

        if (!await _ctx.Providers.AnyAsync(p => p.Name == "Vellon"))
        {
            _ctx.Providers.Add(new Provider { Name = "Vellon" });
            await _ctx.SaveChangesAsync();
        }
        if (!await _ctx.Providers.AnyAsync(p => p.Name == "Wlan"))
        {
            _ctx.Providers.Add(new Provider { Name = "Wlan" });
            await _ctx.SaveChangesAsync();
        }

        await EnsureProviderModelAndCredentialsExact(
           providerName: "Vellon",
           modelName: "Huawei_EG8145V5_V2",
           new[] { ("Epadmin", "alfateste2001") });
        await EnsureProviderModelAndCredentialsExact(
           providerName: "Vellon",
           modelName: "Huawei_EG8145V5_V2",
           new[] { ("Epadmin", "6dTa2dhPYrNdcYhu") });
        await EnsureProviderModelAndCredentialsExact(
           providerName: "Vellon",
           modelName: "Huawei_EG8145V5_V2",
           new[] { ("Epadmin", "adminEp") });

        await EnsureProviderModelAndCredentials(
            providerName: "Wlan",
            modelName: "Huawei_EG8145V5_V2",
            new[] { ("Epadmin", "adminEp"), ("Epadmin", "6dTa2dhPYrNdcYhu") });

        // Admin user (super admin)
        var admin = await _ctx.Users.FirstOrDefaultAsync(u => u.Username == "admin@local");
        if (admin == null)
        {
            admin = new User { Username = "admin@local", PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin@123"), Role = "Admin" };
            _ctx.Users.Add(admin);
            await _ctx.SaveChangesAsync();
        }

        // Tokens fixos: genérico do App, por provedora, e do super admin
        await EnsureGenericAppTokenAsync();
        await EnsureProviderTokensAsync();
        await EnsureAdminTokenAsync(admin);
    }

    private async Task EnsureSchemaCompatAsync()
    {
        var sql = @"
IF COL_LENGTH('dbo.RouterCredentials', 'SortOrder') IS NULL
BEGIN
    ALTER TABLE dbo.RouterCredentials ADD SortOrder INT NOT NULL CONSTRAINT DF_RouterCredentials_SortOrder DEFAULT(0);
END";
        await _ctx.Database.ExecuteSqlRawAsync(sql);
    }

    private async Task EnsureProviderModelAndCredentials(string providerName, string modelName, (string user, string pass)[] list)
    {
        await EnsureSchemaCompatSafeAsync();
        var provider = await _ctx.Providers.FirstOrDefaultAsync(p => p.Name == providerName);
        if (provider == null) return;
        var model = await _ctx.RouterModels.FirstOrDefaultAsync(r => r.ProviderId == provider.Id && r.Name == modelName);
        if (model == null) return;
        List<RouterCredential> current;
        try
        {
            current = await _ctx.RouterCredentials.Where(r => r.RouterModelId == model.Id).ToListAsync();
        }
        catch (SqlException ex) when (ex.Number == 207)
        {
            await EnsureSchemaCompatAsync();
            current = await _ctx.RouterCredentials.Where(r => r.RouterModelId == model.Id).ToListAsync();
        }
        foreach (var c in list)
        {
            if (!current.Any(x => x.Username == c.user))
            {
                _ctx.RouterCredentials.Add(new RouterCredential
                {
                    RouterModelId = model.Id,
                    Username = c.user,
                    PasswordEncrypted = _ctx.Protect(c.pass)
                });
            }
        }
        await _ctx.SaveChangesAsync();
    }

    private async Task EnsureProviderModelAndCredentialsExact(string providerName, string modelName, (string user, string pass)[] list)
    {
        await EnsureSchemaCompatSafeAsync();
        var provider = await _ctx.Providers.FirstOrDefaultAsync(p => p.Name == providerName);
        if (provider == null) return;
        var model = await _ctx.RouterModels.FirstOrDefaultAsync(r => r.ProviderId == provider.Id && r.Name == modelName);
        if (model == null) return;
        List<RouterCredential> current;
        try
        {
            current = await _ctx.RouterCredentials.Where(r => r.RouterModelId == model.Id).ToListAsync();
        }
        catch (SqlException ex) when (ex.Number == 207)
        {
            await EnsureSchemaCompatAsync();
            current = await _ctx.RouterCredentials.Where(r => r.RouterModelId == model.Id).ToListAsync();
        }
        foreach (var c in current)
        {
            _ctx.RouterCredentials.Remove(c);
        }
        foreach (var c in list)
        {
            _ctx.RouterCredentials.Add(new RouterCredential
            {
                RouterModelId = model.Id,
                Username = c.user,
                PasswordEncrypted = _ctx.Protect(c.pass)
            });
        }
        await _ctx.SaveChangesAsync();
    }

    private async Task EnsureSchemaCompatSafeAsync()
    {
        try { await EnsureSchemaCompatAsync(); }
        catch { /* ignore */ }
    }

    private string CreateJwt(IEnumerable<Claim> claims, DateTimeOffset expires)
    {
        var key = _config["Jwt:Key"] ?? throw new InvalidOperationException("Jwt:Key não configurado");
        if (Encoding.UTF8.GetByteCount(key) < 32)
        {
            throw new InvalidOperationException("Jwt:Key precisa ter pelo menos 256 bits (32 bytes).");
        }
        var issuer = _config["Jwt:Issuer"] ?? "RouterManager";
        var creds = new SigningCredentials(new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)), SecurityAlgorithms.HmacSha256);
        var jwt = new JwtSecurityToken(issuer, null, claims, expires: expires.UtcDateTime, signingCredentials: creds);
        return new JwtSecurityTokenHandler().WriteToken(jwt);
    }

    private static string ToHex(byte[] bytes)
    {
        var sb = new StringBuilder(bytes.Length * 2);
        foreach (var b in bytes) sb.Append(b.ToString("x2"));
        return sb.ToString();
    }

    private string ComputeFixedToken(string purpose)
    {
        var key = _config["Jwt:Key"] ?? throw new InvalidOperationException("Jwt:Key não configurado");
        using var h = new HMACSHA256(Encoding.UTF8.GetBytes(key));
        var hash = h.ComputeHash(Encoding.UTF8.GetBytes($"rm:{purpose}"));
        return $"rm_{purpose.Replace(':','_')}_{ToHex(hash)}"; // seguro para header
    }

    private async Task EnsureGenericAppTokenAsync()
    {
        var token = _config["FixedTokens:Generic"] ?? ComputeFixedToken("generic");
        await _tokenStore.UpsertDeviceTokenAsync("GENERIC_APP", token, DateTimeOffset.UtcNow.AddYears(5));
    }

    private async Task EnsureProviderTokensAsync()
    {
        var providers = await _ctx.Providers.AsNoTracking().ToListAsync();
        foreach (var p in providers)
        {
            var cfgKey = $"FixedTokens:Provider:{p.Id}";
            var token = _config[cfgKey] ?? ComputeFixedToken($"provider:{p.Id}");
            await _tokenStore.UpsertProviderTokenAsync(p.Id, token, DateTimeOffset.UtcNow.AddYears(5));
        }
    }

    private async Task EnsureAdminTokenAsync(User admin)
    {
        var token = _config["FixedTokens:Admin"] ?? ComputeFixedToken("admin");
        await _tokenStore.UpsertUserTokenAsync(admin.Id, token, DateTimeOffset.UtcNow.AddYears(5));
    }
}