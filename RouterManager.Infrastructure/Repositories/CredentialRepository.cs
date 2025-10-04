using Microsoft.EntityFrameworkCore;
using RouterManager.Application.Interfaces;
using RouterManager.Domain.Entities;
using RouterManager.Infrastructure.Persistence;

namespace RouterManager.Infrastructure.Repositories;

public class CredentialRepository : ICredentialRepository
{
    private readonly RouterManagerDbContext _ctx;
    public CredentialRepository(RouterManagerDbContext ctx) => _ctx = ctx;

    public async Task<IEnumerable<(string Username, string PasswordPlain)>> GetPlainByProviderAndModelAsync(int providerId, string modelIdentifier, CancellationToken ct = default)
    {
        // Avalia uma única consulta que aceita Name ou EnumIdentifier do modelo
        var hasEnum = Enum.TryParse<RouterModelIdentifier>(modelIdentifier, true, out var parsedEnum);

        var list = await _ctx.RouterCredentials
            .Include(rc => rc.RouterModel)
            .Where(rc => rc.RouterModel.ProviderId == providerId &&
                         (rc.RouterModel.Name == modelIdentifier || (hasEnum && rc.RouterModel.EnumIdentifier == parsedEnum)))
            .Select(rc => new { rc.Username, Plain = _ctx.Unprotect(rc.PasswordEncrypted) })
            .ToListAsync(ct);

        return list.Select(x => (x.Username, x.Plain));
    }
}