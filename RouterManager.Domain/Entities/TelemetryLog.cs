namespace RouterManager.Domain.Entities;

public class TelemetryLog
{
    public long Id { get; set; }
    public Guid DeviceId { get; set; }
    public Device Device { get; set; } = null!;
    public DateTime Timestamp { get; set; }
    public long Uptime { get; set; }
    public double CpuUsage { get; set; }
    public double MemoryUsage { get; set; }
    public int ConnectedClients { get; set; }
}