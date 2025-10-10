namespace RouterManager.Domain.Entities;

public class RouterCredential
{
    public int Id { get; set; }
    public string Username { get; set; } = string.Empty;
    public string PasswordEncrypted { get; set; } = string.Empty;
    public int RouterModelId { get; set; }
    public RouterModel RouterModel { get; set; } = null!;
    public int SortOrder { get; set; } // smaller comes first
}