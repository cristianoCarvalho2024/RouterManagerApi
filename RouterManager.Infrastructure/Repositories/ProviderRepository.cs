using Microsoft.EntityFrameworkCore;
using RouterManager.Application.Interfaces;
using RouterManager.Domain.Entities;
using RouterManager.Infrastructure.Persistence;

namespace RouterManager.Infrastructure.Repositories;

public class ProviderRepository : IProviderRepository
{
    private readonly RouterManagerDbContext _ctx;
    public ProviderRepository(RouterManagerDbContext ctx) => _ctx = ctx;

    public async Task<IEnumerable<Provider>> GetAllAsync(CancellationToken ct = default)
        => await _ctx.Providers.AsNoTracking().OrderBy(p => p.Name).ToListAsync(ct);
}

public class CredentialLookupRepository : ICredentialLookupRepository
{
    private readonly RouterManagerDbContext _ctx;
    public CredentialLookupRepository(RouterManagerDbContext ctx) => _ctx = ctx;

    public async Task<IEnumerable<(string Username, string PasswordPlain)>> GetByProviderAndModelAsync(int providerId, string modelIdentifier, CancellationToken ct = default)
    {
        var creds = await _ctx.RouterCredentials
            .Include(c => c.RouterModel)
            .Where(c => c.RouterModel.ProviderId == providerId && c.RouterModel.EnumIdentifier.ToString() == modelIdentifier)
            .ToListAsync(ct);
        return creds.Select(c => (c.Username, _ctx.Unprotect(c.PasswordEncrypted))).ToList();
    }
}