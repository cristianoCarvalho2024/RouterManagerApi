using Microsoft.EntityFrameworkCore;
using RouterManager.Application.Interfaces;
using RouterManager.Domain.Entities;
using RouterManager.Infrastructure.Persistence;

namespace RouterManager.Infrastructure.Repositories;

public class RouterModelRepository : IRouterModelRepository
{
    private readonly RouterManagerDbContext _ctx;
    public RouterModelRepository(RouterManagerDbContext ctx) => _ctx = ctx;

    public async Task<RouterModel?> GetAsync(int providerId, string modelIdentifier, CancellationToken ct = default)
    {
        // Tenta interpretar como enum; se não der, compara pelo Name
        if (Enum.TryParse<RouterModelIdentifier>(modelIdentifier, out var parsed))
        {
            return await _ctx.RouterModels.FirstOrDefaultAsync(
                m => m.ProviderId == providerId && m.EnumIdentifier == parsed,
                ct);
        }
        else
        {
            return await _ctx.RouterModels.FirstOrDefaultAsync(
                m => m.ProviderId == providerId && m.Name == modelIdentifier,
                ct);
        }
    }
}