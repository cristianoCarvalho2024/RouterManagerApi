using RouterManager.Application.Abstractions;

namespace RouterManager.Infrastructure.Persistence;

public class UnitOfWork : IUnitOfWork
{
    private readonly RouterManagerDbContext _ctx;
    public UnitOfWork(RouterManagerDbContext ctx) => _ctx = ctx;
    public Task<int> SaveChangesAsync(CancellationToken ct = default) => _ctx.SaveChangesAsync(ct);
}