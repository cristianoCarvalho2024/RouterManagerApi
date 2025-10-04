using Microsoft.EntityFrameworkCore;
using RouterManager.Application.Interfaces;
using RouterManager.Domain.Entities;
using RouterManager.Infrastructure.Persistence;

namespace RouterManager.Infrastructure.Repositories;

public class DeviceRepository : IDeviceRepository
{
    private readonly RouterManagerDbContext _ctx;
    public DeviceRepository(RouterManagerDbContext ctx) => _ctx = ctx;

    public async Task<Device> GetOrCreateAsync(string serial, int routerModelId, string firmware, CancellationToken ct = default)
    {
        var device = await _ctx.Devices.FirstOrDefaultAsync(d => d.SerialNumber == serial, ct);
        if (device == null)
        {
            device = new Device { SerialNumber = serial, RouterModelId = routerModelId, FirmwareVersion = firmware, LastSeen = DateTime.UtcNow };
            _ctx.Devices.Add(device);
            await _ctx.SaveChangesAsync(ct);
        }
        return device;
    }

    public async Task UpdateAsync(Device device, CancellationToken ct = default)
    {
        _ctx.Devices.Update(device);
        await _ctx.SaveChangesAsync(ct);
    }
}