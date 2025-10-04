using RouterManager.Application.Interfaces;
using RouterManager.Domain.Entities;

namespace RouterManager.Application.Services;

public class TelemetryService : ITelemetryService
{
    private readonly ITelemetryRepository _telemetryRepo;
    private readonly IRouterModelRepository _modelRepo;
    private readonly IDeviceRepository _deviceRepo;
    public TelemetryService(ITelemetryRepository telemetryRepo, IRouterModelRepository modelRepo, IDeviceRepository deviceRepo)
    {
        _telemetryRepo = telemetryRepo; _modelRepo = modelRepo; _deviceRepo = deviceRepo;
    }

    public async Task ReportAsync(Shared.Dtos.Requests.ReportStatusRequest request, CancellationToken ct = default)
    {
        var model = await _modelRepo.GetAsync(request.ProviderId, request.ModelIdentifier, ct);
        if (model == null) return; // silently ignore invalid
        var device = await _deviceRepo.GetOrCreateAsync(request.SerialNumber, model.Id, request.FirmwareVersion, ct);
        device.LastSeen = DateTime.UtcNow;
        device.FirmwareVersion = request.FirmwareVersion;
        await _deviceRepo.UpdateAsync(device, ct);
        var log = new TelemetryLog
        {
            DeviceId = device.Id,
            Timestamp = DateTime.UtcNow,
            Uptime = request.Uptime,
            CpuUsage = request.CpuUsage,
            MemoryUsage = request.MemoryUsage,
            ConnectedClients = request.ConnectedClients
        };
        await _telemetryRepo.AddLogAsync(log, ct);
    }
}