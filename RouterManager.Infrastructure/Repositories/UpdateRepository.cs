using Microsoft.EntityFrameworkCore;
using RouterManager.Application.Interfaces;
using RouterManager.Domain.Entities;
using RouterManager.Infrastructure.Persistence;

namespace RouterManager.Infrastructure.Repositories;

public class UpdateRepository : IUpdateRepository
{
    private readonly RouterManagerDbContext _ctx;
    public UpdateRepository(RouterManagerDbContext ctx) => _ctx = ctx;

    public async Task<UpdatePackage?> GetApplicableAsync(int providerId, string modelIdentifier, string firmwareVersion, CancellationToken ct = default)
    {
        // Mantido para compatibilidade, usa a priorização por SerialNumber != null
        return await _ctx.UpdatePackages
            .AsNoTracking()
            .Where(p => p.ProviderId == providerId
                && (p.ModelIdentifier == modelIdentifier || p.ModelIdentifier == "*")
                && (p.FirmwareVersion == null || p.FirmwareVersion == firmwareVersion))
            .OrderByDescending(p => p.SerialNumber != null)
            .ThenByDescending(p => p.Id)
            .FirstOrDefaultAsync(ct);
    }

    public async Task<UpdatePackage?> FindSpecificAsync(int providerId, string modelIdentifier, string serialNumber, CancellationToken ct = default)
    {
        return await _ctx.UpdatePackages.AsNoTracking().FirstOrDefaultAsync(p =>
            p.ProviderId == providerId &&
            p.ModelIdentifier == modelIdentifier &&
            p.SerialNumber == serialNumber, ct);
    }

    public async Task<UpdatePackage?> FindGenericAsync(int providerId, string modelIdentifier, string? firmwareVersion, CancellationToken ct = default)
    {
        return await _ctx.UpdatePackages.AsNoTracking()
            .Where(p => p.ProviderId == providerId
                && p.ModelIdentifier == modelIdentifier
                && p.SerialNumber == null
                && (p.FirmwareVersion == null || p.FirmwareVersion == firmwareVersion))
            .OrderByDescending(p => p.Id)
            .FirstOrDefaultAsync(ct);
    }
}