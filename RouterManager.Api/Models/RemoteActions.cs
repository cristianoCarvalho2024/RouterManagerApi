using System.Text.Json;
using System.Text.Json.Serialization;

namespace RouterManager.Api.Models;

public static class RemoteActionTypes
{
    public const string HttpDownload = "HttpDownload";
    public const string RouterCommand = "RouterCommand";
}

public sealed class RemoteActionEnvelope
{
    [JsonPropertyName("ActionType")] public string ActionType { get; set; } = string.Empty;
    [JsonPropertyName("Payload")] public JsonElement Payload { get; set; }
}

public sealed class HttpDownloadPayload
{
    public string Url { get; set; } = string.Empty;
    public string Method { get; set; } = "GET";
    public Dictionary<string, string>? Headers { get; set; }
    public JsonElement? Body { get; set; }
    public int? TimeoutSeconds { get; set; }
}

public sealed class RouterCommandPayload
{
    public string Command { get; set; } = string.Empty;
    public string TargetService { get; set; } = string.Empty;
    public JsonElement Parameters { get; set; }
}
