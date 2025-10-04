using RouterManager.Application.Interfaces;
using RouterManager.Domain.Entities;
using RouterManager.Infrastructure.Persistence;

namespace RouterManager.Infrastructure.Repositories;

public class TelemetryRepository : ITelemetryRepository
{
    private readonly RouterManagerDbContext _ctx;
    public TelemetryRepository(RouterManagerDbContext ctx) => _ctx = ctx;

    public async Task AddLogAsync(TelemetryLog log, CancellationToken ct = default)
    {
        _ctx.TelemetryLogs.Add(log);
        await _ctx.SaveChangesAsync(ct);
    }
}