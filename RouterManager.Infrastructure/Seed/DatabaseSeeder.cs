using RouterManager.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using RouterManager.Domain.Entities;
using BCrypt.Net;

namespace RouterManager.Infrastructure.Seed;

public interface IDatabaseSeeder
{
    Task SeedAsync();
}

public class DatabaseSeeder : IDatabaseSeeder
{
    private readonly RouterManagerDbContext _ctx;
    public DatabaseSeeder(RouterManagerDbContext ctx) => _ctx = ctx;

    public async Task SeedAsync()
    {
        // Executa migrações somente quando o provedor é relacional (não InMemory)
        if (_ctx.Database.IsRelational())
        {
            await _ctx.Database.MigrateAsync();
        }

        if (!await _ctx.Providers.AnyAsync())
        {
            var provider = new Provider { Name = "Default ISP" };
            _ctx.Providers.Add(provider);
            await _ctx.SaveChangesAsync();

            var model = new RouterModel { Name = "AX1000", EnumIdentifier = RouterModelIdentifier.ModelAX1000, ProviderId = provider.Id };
            _ctx.RouterModels.Add(model);
            await _ctx.SaveChangesAsync();

            var cred = new RouterCredential { Username = "admin", PasswordEncrypted = _ctx.Protect("admin123"), RouterModelId = model.Id };
            _ctx.RouterCredentials.Add(cred);
            await _ctx.SaveChangesAsync();
        }

        if (!await _ctx.Users.AnyAsync())
        {
            var user = new User { Username = "admin@local", PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin#123"), Role = "Admin" };
            _ctx.Users.Add(user);
            await _ctx.SaveChangesAsync();
        }
    }
}