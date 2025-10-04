using System.Text.Json.Serialization;

namespace RouterManager.Shared.Dtos.Responses;

public class UpdatePackageResponse
{
    [JsonPropertyName("updateId")] public string UpdateId { get; set; } = string.Empty;
    [JsonPropertyName("targetVersion")] public string TargetVersion { get; set; } = string.Empty;
    [JsonPropertyName("description")] public string Description { get; set; } = string.Empty;
    [JsonPropertyName("updateActions")] public List<UpdateActionItem> Actions { get; set; } = new();
}

public record UpdateActionItem(int Order, string Service, string Method, string? ParamsJson);