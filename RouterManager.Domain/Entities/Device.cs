namespace RouterManager.Domain.Entities;

public class Device
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string SerialNumber { get; set; } = string.Empty;
    public string FirmwareVersion { get; set; } = string.Empty;
    public int RouterModelId { get; set; }
    public RouterModel RouterModel { get; set; } = null!;
    public DateTime LastSeen { get; set; }
    public ICollection<TelemetryLog> TelemetryLogs { get; set; } = new List<TelemetryLog>();
}