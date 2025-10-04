using Microsoft.EntityFrameworkCore;
using RouterManager.Application.Interfaces;
using RouterManager.Domain.Entities;
using RouterManager.Infrastructure.Persistence;

namespace RouterManager.Infrastructure.Repositories;

public class RouterModelRepository : IRouterModelRepository
{
    private readonly RouterManagerDbContext _ctx;
    public RouterModelRepository(RouterManagerDbContext ctx) => _ctx = ctx;

    public Task<RouterModel?> GetAsync(int providerId, string modelIdentifier, CancellationToken ct = default)
        => _ctx.RouterModels.FirstOrDefaultAsync(m => m.ProviderId == providerId && m.EnumIdentifier.ToString() == modelIdentifier, ct);
}