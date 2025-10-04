using System.Text.Json.Serialization;

namespace RouterManager.Shared.Dtos.Requests;

public record ReportStatusRequest(
    [property: JsonPropertyName("serialNumber")] string SerialNumber,
    [property: JsonPropertyName("firmwareVersion")] string FirmwareVersion,
    [property: JsonPropertyName("modelIdentifier")] string ModelIdentifier,
    [property: JsonPropertyName("providerId")] int ProviderId,
    [property: JsonPropertyName("uptime")] long Uptime,
    [property: JsonPropertyName("cpuUsage")] double CpuUsage,
    [property: JsonPropertyName("memoryUsage")] double MemoryUsage,
    [property: JsonPropertyName("connectedDevicesCount")] int ConnectedClients
);