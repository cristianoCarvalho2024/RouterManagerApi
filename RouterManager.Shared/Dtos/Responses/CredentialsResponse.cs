namespace RouterManager.Shared.Dtos.Responses;

public class CredentialsResponse
{
    public int ProviderId { get; set; }
    public string Model { get; set; } = string.Empty;
    public List<CredentialItem> Credentials { get; set; } = new();
}

public record CredentialItem(string Username, string Password);